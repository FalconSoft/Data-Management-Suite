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
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class RabbitMQFacadeBase : IServerNotification
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _hasConnection;
        private const int TimeOut = 2000;

        public RabbitMQFacadeBase(string hostName, string userName, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public event EventHandler<ServerErrorEvArgs> ServerErrorHandler;

        protected IConnection Connection {get { return _connection; }}

        protected IModel CommandChannel { get { return _commandChannel; } }
        
        protected IEnumerable<T> RPCServerTaskExecuteEnumerable<T>(IConnection connection,
          string commandQueueName, string methodName, string userToken, object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                    InitializeConnection(commandQueueName);

                var channel = connection.CreateModel();

                var correlationId = Guid.NewGuid().ToString();

                var consumer = new QueueingBasicConsumer(channel);

                var replyTo = channel.QueueDeclare().QueueName;
                channel.BasicConsume(replyTo, true, consumer);

                var props = channel.CreateBasicProperties();
                props.ReplyTo = replyTo;
                props.CorrelationId = correlationId;
                props.SetPersistent(true);

                var message = MethdoArgsToByte(methodName, userToken, methodArgs);

                channel.BasicPublish("", commandQueueName, props, message);

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
                    ServerErrorHandler(this, new ServerErrorEvArgs("Connection to server has been lost!", ex));

                return default(IEnumerable<T>);
            }
        }

        protected T RPCServerTaskExecute<T>(IConnection connection, string commandQueueName, string methodName, string userToken,
           object[] methodArgs)
        {
            try
            {
                if (!_hasConnection)
                    InitializeConnection(commandQueueName);

                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();

                    var queueName = channel.QueueDeclare().QueueName;

                    var props = channel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = queueName;
                    props.SetPersistent(true);

                    var consumer = new QueueingBasicConsumer(channel);

                    channel.BasicConsume(queueName, false, consumer);

                    var message = new MethodArgs
                    {
                        MethodName = methodName,
                        UserToken = userToken,
                        MethodsArgs = methodArgs
                    };

                    var messageBytes = BinaryConverter.CastToBytes(message);

                    channel.BasicPublish("", commandQueueName, props, messageBytes);

                    while (true)
                    {
                        BasicDeliverEventArgs ea;
                        if (consumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (ea.BasicProperties.CorrelationId == correlationId)
                            {
                                channel.QueueDelete(queueName);
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
                    InitializeConnection(commandQueueName);

                using (var channel = connection.CreateModel())
                {
                    var correlationId = Guid.NewGuid().ToString();

                    var queueName = channel.QueueDeclare().QueueName;

                    var props = channel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = queueName;
                    props.SetPersistent(true);

                    var consumer = new QueueingBasicConsumer(channel);

                    channel.BasicConsume(queueName, false, consumer);

                    var message = new MethodArgs
                    {
                        MethodName = methodName,
                        UserToken = userToken,
                        MethodsArgs = methodArgs
                    };

                    var messageBytes = BinaryConverter.CastToBytes(message);

                    channel.BasicPublish("", commandQueueName, props, messageBytes);

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

        protected IObservable<T> CreateExchngeObservable<T>(IModel channel, string exchangeName,
            string exchangeType, string routingKey)
        {
            var subjects = new Subject<T>();

            CommandChannel.ExchangeDeclare(exchangeName, exchangeType);

            var queueName = CommandChannel.QueueDeclare().QueueName;
            CommandChannel.QueueBind(queueName, exchangeName, routingKey);

            var con = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(queueName, true, con);

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

                        var data = (T) responce.Data;

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
            using (var channel = _connection.CreateModel())
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
                    throw new Exception("Connection to server failed");
                }
            }
        }

        protected void Close()
        {
            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
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
