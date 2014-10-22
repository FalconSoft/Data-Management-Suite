using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Timer = System.Timers.Timer;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    internal sealed class RabbitMQClient : IRabbitMQClient, IServerNotification
    {
        private IConnection _connection;
        private IModel _commandChannel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Timer _timer;
        private readonly ConnectionFactory _factory;
        private readonly string _replyTo;
        private readonly QueueingBasicConsumer _consumer;
        private const string CommandQueueName = "CommandQueue";

        public RabbitMQClient(string hostName, string userName, string password, string virtualHost)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            RestoreConnection();

            _replyTo = _commandChannel.QueueDeclare().QueueName;

            _timer = new Timer();
            _timer.Interval = 3000;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();

            _consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(_replyTo, false, _consumer);

           
        }

        private void CheckConnection()
        {
            if (_commandChannel.IsClosed)
            {
                RestoreConnection();
            }
            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = _replyTo;
            _commandChannel.BasicPublish("", CommandQueueName, props, null);

            try
            {
                BasicDeliverEventArgs eventArgs;
                if (_consumer.Queue.Dequeue(3000, out eventArgs))
                {
                    if (ServerReconnectedEvent != null)
                        ServerReconnectedEvent(this, new ServerReconnectionArgs());

                }
                else
                {
                    if (ServerReconnectedEvent != null)
                        ServerErrorHandler(this,
                            new ServerErrorEvArgs("Connection to server lost", new NullReferenceException()));
                }
            }
            catch (AlreadyClosedException ex)
            {

                if (ServerReconnectedEvent != null)
                    ServerErrorHandler(this,
                        new ServerErrorEvArgs("Connection to server lost", new NullReferenceException()));

                try
                {
                    RestoreConnection();
                }
                catch (BrokerUnreachableException)
                {

                }
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            //CheckConnection();
        }

        public void SubscribeOnExchange<T>(string exchangeName, string exchangeType, string routingKey, Action<T> action)
        {
            _commandChannel.ExchangeDeclare(exchangeName, exchangeType);

            string queueName = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueName, exchangeName, routingKey);

            var con = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, true, con);

            Task.Factory.StartNew(obj =>
            {
                var consumer = (QueueingBasicConsumer)obj;
                while (true)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var responce = CastTo<T>(ea.Body);

                        if (action != null)
                            action(responce);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }, con, _cts.Token);
        }

        public void SubscribeOnExchange(string exchangeName, string exchangeType, string routingKey, Action<string, string> action)
        {
            _commandChannel.ExchangeDeclare(exchangeName, exchangeType);

            var queueNameForExceptions = _commandChannel.QueueDeclare().QueueName;

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumerForExceptions);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumerForExceptions.Queue.Dequeue();

                        var array = CastTo<string>(ea.Body).Split(new[] { "[#]" }, StringSplitOptions.None);

                        if (action != null)
                            action(array[0], array[1]);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }, _cts.Token);
        }

        public IObservable<T> CreateExchngeObservable<T>(string exchangeName,
            string exchangeType, string routingKey)
        {
            var subjects = CreateSubject<T>(exchangeName, exchangeType, routingKey);

            var func = new Func<IObserver<T>, IDisposable>(subj =>
            {
                var dispoce = subjects.Subscribe(subj);

                return Disposable.Create(dispoce.Dispose);
            });

            return Observable.Create(func);
        }

        private IObservable<T> CreateSubject<T>(string exchangeName,
            string exchangeType, string routingKey)
        {
            var subjects = new Subject<T>();

            _commandChannel.ExchangeDeclare(exchangeName, exchangeType);

            string queueName = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueName, exchangeName, routingKey);

            var con = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, true, con);

            var taskComplete = true;

            Task.Factory.StartNew(obj =>
            {
                var consumer = (QueueingBasicConsumer)obj;
                while (taskComplete)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var responce = CastTo<T>(ea.Body);

                        subjects.OnNext(responce);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }, con, _cts.Token);

            return Observable.Create<T>(subj =>
            {
                var dispoce = subjects.Subscribe(subj);

                return Disposable.Create(() =>
                {
                    if (_commandChannel.IsOpen)
                        _commandChannel.QueueUnbind(queueName, exchangeName, routingKey, null);
                    con.OnCancel();
                    dispoce.Dispose();
                    taskComplete = false;
                });
            });
        }

        public void Close()
        {
            _timer.Stop();
            _timer.Dispose();
            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
        }

        private void RestoreConnection()
        {
            _connection = _factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        private T CastTo<T>(byte[] byteArray)
        {
            if (!byteArray.Any())
                return default(T);

            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return (T)binForm.Deserialize(memStream);
            }
        }

        public event EventHandler<ServerErrorEvArgs> ServerErrorHandler;

        public event EventHandler<ServerReconnectionArgs> ServerReconnectedEvent;
    }

    public interface IRabbitMQClient
    {
        void SubscribeOnExchange<T>(string exchangeName, string exchangeType, string routingKey, Action<T> action);

        void SubscribeOnExchange(string exchangeName, string exchangeType, string routingKey,
            Action<string, string> action);

        IObservable<T> CreateExchngeObservable<T>(string exchangeName,
            string exchangeType, string routingKey);

        void Close();
    }
}
