using System;
using System.Collections.Generic;
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

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class RabbitMQFacadeBase : IServerNotification
    {
        private IConnection _connection;
        private IModel _commandChannel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _hasConnection;
        private const int TimeOut = 2000;
        private readonly Timer _keepAliveTimer;

        public RabbitMQFacadeBase(string hostName, string userName, string password)
        {
            factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            RestoreConnection();

            _keepAliveTimer = new Timer(5000);
            _keepAliveTimer.Elapsed += KeepAlive;
            _keepAliveTimer.Start();
        }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
            if (!_hasConnection && KeepAliveAction != null)
            {
                KeepAliveAction();
                if (_hasConnection && ServerReconnectedEvent != null)
                    ServerReconnectedEvent(this, new ServerReconnectionArgs());
            }
            else
            {
                if (KeepAliveAction != null)
                    KeepAliveAction();
            }
        }

        public event EventHandler<ServerErrorEvArgs> ServerErrorHandler;

        public event EventHandler<ServerReconnectionArgs> ServerReconnectedEvent;

        protected IConnection Connection { get { return _connection; } }

        protected IModel CommandChannel { get { return _commandChannel; } }

        protected Action KeepAliveAction;
        private readonly object _restoreConnectionLock = new object();
        private ConnectionFactory factory;

        protected IEnumerable<T> RPCServerTaskExecuteEnumerable<T>(IConnection connection,
          string commandQueueName, string methodName, string userToken, object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                    return default(IEnumerable<T>);

                var channel = connection.CreateModel();

                var correlationId = Guid.NewGuid().ToString();

                var consumer = SendMessage(channel, commandQueueName, methodName, userToken, methodArgs, correlationId);

                var subject = new Subject<T>();

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (correlationId == ea.BasicProperties.CorrelationId)
                            {
                                var responce = CastTo<RabbitMQResponce>(ea.Body);

                                if (responce.LastMessage)
                                {
                                    channel.Dispose();
                                    break;
                                }

                                var list = (List<T>)responce.Data;
                                foreach (var dictionary in list)
                                {
                                    subject.OnNext(dictionary);
                                }
                            }
                        }
                        else
                        {
                            channel.Dispose();
                            _hasConnection = false;
                            break;
                        }
                    }
                }, _cts.Token)
                .ContinueWith(t => subject.OnCompleted());

                return subject.ToEnumerable();
            }
            catch (Exception ex)
            {
                if (ServerErrorHandler != null)
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex) { MethodCallerName = methodName });

                return default(IEnumerable<T>);
            }
        }

        protected T RPCServerTaskExecute<T>(IConnection connection, string commandQueueName, string methodName, string userToken,
           object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                {
                    if (typeof (T).IsArray)
                        return (T)(object)Array.CreateInstance(typeof (T).GetElementType(), 0);

                    return default(T);
                }

                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();

                    var consumer = SendMessage(channel, commandQueueName, methodName, userToken, methodArgs, correlationId);

                    while (true)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (ea.BasicProperties.CorrelationId == correlationId)
                            {
                                return BinaryConverter.CastTo<T>(ea.Body);
                            }
                        }
                        else
                        {
                            _hasConnection = false;
                            return default(T);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ServerErrorHandler != null)
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex));

                return default(T);
            }
        }

        protected void RPCServerTaskExecute(IConnection connection, string commandQueueName, string methodName, string userToken,
            object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                    return;

                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();

                    var consumer = SendMessage(channel, commandQueueName, methodName, userToken, methodArgs, correlationId);

                    while (true)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (ea.BasicProperties.CorrelationId == correlationId)
                            {
                                break;
                            }
                        }
                        else
                        {
                            _hasConnection = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ServerErrorHandler != null)
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex));
            }
        }

        protected void RPCServerTaskExecuteAsync(IConnection connection, string commandQueueName, string methodName, string userToken,
           object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                    return;

                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();

                    SendMessage(channel, commandQueueName, methodName, userToken, methodArgs, correlationId);
                }
            }
            catch (Exception ex)
            {
                if (ServerErrorHandler != null)
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex));
            }
        }

        protected IObservable<T> CreateExchngeObservable<T>(IModel channel, string exchangeName,
            string exchangeType, string routingKey, out string bindingQueueName)
        {
            var subjects = new Subject<T>();

            channel.ExchangeDeclare(exchangeName, exchangeType);

            var queueName = CommandChannel.QueueDeclare().QueueName;
            channel.QueueBind(queueName, exchangeName, routingKey);

            bindingQueueName = queueName;

            var con = new QueueingBasicConsumer(CommandChannel);
            channel.BasicConsume(queueName, true, con);

            var taskComplete = true;

            Task.Factory.StartNew(obj =>
            {
                var consumer = (QueueingBasicConsumer)obj;
                while (taskComplete)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var responce = BinaryConverter.CastTo<RabbitMQResponce>(ea.Body);

                        if (responce.LastMessage) break;

                        var data = (T)responce.Data;

                        subjects.OnNext(data);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }, con, _cts.Token);

            var func = new Func<IObserver<T>, IDisposable>(subj =>
            {
                var dispoce = subjects.Subscribe(subj);

                return Disposable.Create(() =>
                {
                    CommandChannel.QueueUnbind(queueName, exchangeName, routingKey, null);
                    con.OnCancel();
                    dispoce.Dispose();
                    taskComplete = false;
                });
            });

            return Observable.Create(func);
        }

        protected void InitializeConnection(string commandQueueName)
        {
            try
            {
                CheckConnection(commandQueueName);
            }
            catch (Exception ex)
            {
                if (ex is AlreadyClosedException)
                    RestoreConnection();

                if (ServerErrorHandler != null)
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex));
            }
        }

        private void RestoreConnection()
        {
            lock (_restoreConnectionLock)
            {
                _hasConnection = false;
                _connection = factory.CreateConnection();
                _commandChannel = _connection.CreateModel();
            }
        }

        protected void Close()
        {
            _cts.Cancel();
            _cts.Dispose();
            _keepAliveTimer.Close();
            _commandChannel.Close();
            _connection.Close();
        }

        private QueueingBasicConsumer SendMessage(IModel channel, string commandQueueName, string methodName, string userToken, object[] methodArgs, string correlationId)
        {
            var consumer = new QueueingBasicConsumer(channel);

            var replyTo = channel.QueueDeclare().QueueName;
            channel.BasicConsume(replyTo, true, consumer);

            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;
            props.SetPersistent(true);

            var message = MethdoArgsToByte(methodName, userToken, methodArgs);

            channel.BasicPublish("", commandQueueName, props, message);

            return consumer;
        }

        private void CheckConnection(string commandQueueName)
        {
            IModel channel = null;
            try
            {
                channel = _connection.CreateModel();
                if (channel != null)
                {
                    var message = new MethodArgs
                    {
                        MethodName = "InitializeConnection",
                        UserToken = null,
                        MethodsArgs = null
                    };

                    var messageBytes = BinaryConverter.CastToBytes(message);

                    var replyTo = channel.QueueDeclare().QueueName;

                    var correlationId = Guid.NewGuid().ToString();

                    var props = channel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;
                    props.SetPersistent(true);

                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(replyTo, true, consumer);

                    channel.BasicPublish("", commandQueueName, props, messageBytes);

                    while (true)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (correlationId == ea.BasicProperties.CorrelationId)
                            {
                                _hasConnection = true;
                                return;
                            }
                        }
                        _hasConnection = false;
                        throw new TimeoutException("Connection to server failed due to time out !");
                    }
                }
                else
                {
                    _hasConnection = false;
                    throw new NullReferenceException("Cannot connect to server!");
                }
            }
           
            finally
            {
                if (channel != null)
                    channel.Dispose();
            }
        }

        private byte[] MethdoArgsToByte(string methodName, string userToken, object[] methodArgs)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, new MethodArgs
            {
                MethodName = methodName,
                UserToken = userToken,
                MethodsArgs = methodArgs
            });

            return ms.ToArray();
        }

        private T CastTo<T>(byte[] byteArray)
        {
            if (!byteArray.Any())
                return default(T);

            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return (T)binForm.Deserialize(memStream);
        }
    }
}
