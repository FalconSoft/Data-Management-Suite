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
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyTo;

            var message = new MethodArgs
            {
                MethodName = "GetUserPermissions",
                UserToken = userToken
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", PermissionSecurityFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, false, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var permission = BinaryConverter.CastTo<Permission>(ea.Body);

                    return permission;
                }
            }
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
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyTo;

            var message = new MethodArgs
            {
                MethodName = "CheckAccess",
                UserToken = userToken,
                MethodsArgs = new object[] { urn }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", PermissionSecurityFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, false, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var accessLevel = BinaryConverter.CastTo<AccessLevel>(ea.Body);

                    return accessLevel;
                }
            }
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
                    dispoce.Dispose();
                    taskComplete = false;
                });
            });

            return Observable.Create(func);
        }

        public void Dispose()
        {
            _commandChannel.Dispose();
            _connection.Dispose();
        }
    }
}