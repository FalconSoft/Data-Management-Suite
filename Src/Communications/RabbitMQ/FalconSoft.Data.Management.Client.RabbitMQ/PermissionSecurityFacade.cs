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
    internal sealed class PermissionSecurityFacade : RabbitMQFacadeBase, IPermissionSecurityFacade
    {
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private const int TimeOut = 2000;

        public PermissionSecurityFacade(string hostName, string userName, string password):base(hostName, userName,password)
        {
            InitializeConnection(PermissionSecurityFacadeQueueName);
        }

        public Permission GetUserPermissions(string userToken)
        {
            return RPCServerTaskExecute<Permission>(Connection, PermissionSecurityFacadeQueueName, "GetUserPermissions",
                userToken, null);
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = CommandChannel.QueueDeclare().QueueName;

            var props = CommandChannel.CreateBasicProperties();
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

            CommandChannel.BasicPublish("", PermissionSecurityFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(replyTo, false, consumer);

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
            return RPCServerTaskExecute<AccessLevel>(Connection, PermissionSecurityFacadeQueueName, "CheckAccess",
                userToken, new object[] { urn });
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            var observable = CreateExchngeObservable<Dictionary<string, AccessLevel>>(CommandChannel,
                PermissionSecurityFacadeExchangeName, "direct", userToken);

            RPCServerTaskExecute(Connection, PermissionSecurityFacadeQueueName, "GetPermissionChanged", userToken, null);

            return observable;
        }

        public void Dispose()
        {
           
        }

        public new void Close()
        {
            _cts.Cancel();
            base.Close();
        }

    }
}