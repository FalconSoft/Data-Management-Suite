using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class SecurityFacade : RabbitMQFacadeBase, ISecurityFacade
    {
        private const string SecurityFacadeQueueName = "SecurityFacadeRPC";
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private QueueingBasicConsumer _consumerForExceptions;
        private string _queueNameForExceptions;
        private readonly EventHandler<ServerReconnectionArgs> _exhcangeKeepAlive;

        public SecurityFacade(string hostName, string userName, string password) : base(hostName, userName, password)
        {
            InitializeConnection(SecurityFacadeQueueName);

            KeepAliveAction= ()=> InitializeConnection(SecurityFacadeQueueName);

            InitializeFanoutExchanges();

            _exhcangeKeepAlive = (obj, evArgs) => InitializeFanoutExchanges();

            ServerReconnectedEvent += _exhcangeKeepAlive;
        }

        private void InitializeFanoutExchanges()
        {
            CommandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            _queueNameForExceptions = CommandChannel.QueueDeclare().QueueName;
            CommandChannel.QueueBind(_queueNameForExceptions, ExceptionsExchangeName, "");

            _consumerForExceptions = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(_queueNameForExceptions, false, _consumerForExceptions);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = _consumerForExceptions.Queue.Dequeue();

                        var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                        if (ErrorMessageHandledAction != null)
                            ErrorMessageHandledAction(array[0], array[1]);
                    }
                    catch (EndOfStreamException)
                    {
                        return;
                    }
                }
            }, _cts.Token);
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            return RPCServerTaskExecute<KeyValuePair<bool, string>>(Connection, SecurityFacadeQueueName, "Authenticate",
                null, new object[] { userName, password });
        }

        public List<User> GetUsers(string userToken)
        {
            return RPCServerTaskExecute<List<User>>(Connection, SecurityFacadeQueueName, "GetUsers", userToken, null);
        }

        public User GetUser(string userName)
        {
            return RPCServerTaskExecute<User>(Connection, SecurityFacadeQueueName, "GetUser", null,
                new object[] { userName });
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            return RPCServerTaskExecute<string>(Connection, SecurityFacadeQueueName, "SaveNewUser", userToken,
                new object[] { user, userRole });
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            RPCServerTaskExecute(Connection, SecurityFacadeQueueName, "UpdateUser", userToken, new object[] { user, userRole });

        }

        public void RemoveUser(User user, string userToken)
        {
            RPCServerTaskExecute(Connection, SecurityFacadeQueueName, "RemoveUser", userToken, new object[] { user });
        }

        public void Dispose()
        {

        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
        
        public new void Close()
        {
            CommandChannel.QueueUnbind(_queueNameForExceptions, ExceptionsExchangeName, "", null);
            _cts.Cancel();
            _cts.Dispose();

            ServerReconnectedEvent -= _exhcangeKeepAlive;

            base.Close();
        }

    }
}
