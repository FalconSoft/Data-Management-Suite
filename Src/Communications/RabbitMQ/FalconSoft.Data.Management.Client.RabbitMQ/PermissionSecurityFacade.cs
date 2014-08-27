using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class PermissionSecurityFacade : IPermissionSecurityFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private const int TimeOut = 5000;

        public PermissionSecurityFacade(string serverUrl, string userName, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = serverUrl,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
            
            InitializeConnection(PermissionSecurityFacadeQueueName);
        }

        public Permission GetUserPermissions(string userToken)
        {
            return RPCServerTaskExecute<Permission>(_connection, PermissionSecurityFacadeQueueName, "GetUserPermissions",
                userToken, null);
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyTo;
            props.SetPersistent(true);

            var message = new MethodArgs
            {
                MethodName = "SaveUserPermissions",
                UserToken = grantedByUserToken,
                MethodsArgs = new object[] { permissions, targetUserToken }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", PermissionSecurityFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, false, consumer);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue(TimeOut, out ea))
                    {

                        if (correlationId == ea.BasicProperties.CorrelationId)
                        {
                            var responceMessage = BinaryConverter.CastTo<string>(ea.Body);

                            if (messageAction != null)
                                messageAction(responceMessage);

                            break;
                        }
                    }
                    else
                    {
                        if (messageAction != null)
                            messageAction("Aborted connection to server!");

                        break;
                    }
                }
            }, _cts.Token);
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            return RPCServerTaskExecute<AccessLevel>(_connection, PermissionSecurityFacadeQueueName, "CheckAccess",
                userToken, new object[] { urn });
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            var subjects = new Subject<Dictionary<string, AccessLevel>>();

            _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

            var queueName = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueName, PermissionSecurityFacadeExchangeName, userToken);


            var message = new MethodArgs
            {
                MethodName = "GetPermissionChanged",
                UserToken = userToken
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", PermissionSecurityFacadeQueueName, null, messageBytes);

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

                        var data = BinaryConverter.CastTo<Dictionary<string, AccessLevel>>(ea.Body);

                        subjects.OnNext(data);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }, con, _cts.Token);

            var func = new Func<IObserver<Dictionary<string, AccessLevel>>, IDisposable>(subj =>
            {
                var dispoce = subjects.Subscribe(subj);

                return Disposable.Create(() =>
                {
                    _commandChannel.QueueUnbind(queueName, PermissionSecurityFacadeExchangeName, userToken, null);
                    con.OnCancel();
                    dispoce.Dispose();
                    taskComplete = false;
                });
            });

            return Observable.Create(func);
        }

        public void Dispose()
        {
           
        }

        private T RPCServerTaskExecute<T>(IConnection connection, string commandQueueName, string methodName, string userToken,
            object[] methodArgs)
        {
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
                        return default(T);
                    }
                }
            }
        }

        private void InitializeConnection(string commandQueueName)
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
                            return;
                        }
                    }
                    throw new Exception("Connection to server failed");
                }
            }
        }

        public void Close()
        {
            _cts.Cancel();
            _commandChannel.Close();
            _connection.Close();
        }

    }
}