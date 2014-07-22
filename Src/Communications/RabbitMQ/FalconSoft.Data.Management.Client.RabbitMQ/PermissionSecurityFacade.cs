using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class PermissionSecurityFacade : IPermissionSecurityFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";

        public PermissionSecurityFacade(string serverUrl)
        {
            var factory = new ConnectionFactory { HostName = serverUrl };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
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
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var responceMessage = BinaryConverter.CastTo<string>(ea.Body);

                        if (messageAction != null)
                            messageAction(responceMessage);

                        break;
                    }
                }
            });
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            return RPCServerTaskExecute<AccessLevel>(_connection, PermissionSecurityFacadeQueueName, "CheckAccess",
                userToken, new object[] {urn});
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
                var consumer = (QueueingBasicConsumer) obj;
                while (taskComplete)
                {
                    var ea = consumer.Queue.Dequeue();
                    
                    var data = BinaryConverter.CastTo<Dictionary<string, AccessLevel>>(ea.Body);

                    subjects.OnNext(data);
                }
            }, con);

            var func = new Func<IObserver<Dictionary<string, AccessLevel>>, IDisposable>(subj =>
            {
                var dispoce = subjects.Subscribe(subj);

                return Disposable.Create(() =>
                {
                    con.OnCancel();
                    dispoce.Dispose();
                    taskComplete = false;
                });
            });

            return Observable.Create(func);
        }

        public void Dispose()
        {
            //_commandChannel.Dispose();
            //_connection.Dispose();
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
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<T>(ea.Body);
                    }
                }
            }
        }
    }
}