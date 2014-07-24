using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class SecurityFacade : ISecurityFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string SecurityFacadeQueueName = "SecurityFacadeRPC";
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";
        
        public SecurityFacade(string hostName)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = "RWClient",
                Password = "RWClient",
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            var queueNameForExceptions = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueNameForExceptions, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumerForExceptions);
            
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumerForExceptions.Queue.Dequeue();
                    
                    var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                    if (ErrorMessageHandledAction != null)
                        ErrorMessageHandledAction(array[0], array[1]);
                }
            });
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            return RPCServerTaskExecute<KeyValuePair<bool, string>>(_connection, SecurityFacadeQueueName, "Authenticate",
                null, new object[] {userName, password});
        }

        public List<User> GetUsers(string userToken)
        {
            return RPCServerTaskExecute<List<User>>(_connection, SecurityFacadeQueueName, "GetUsers", userToken, null);
        }

        public User GetUser(string userName)
        {
            return RPCServerTaskExecute<User>(_connection, SecurityFacadeQueueName, "GetUser", null,
                new object[] {userName});
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            return RPCServerTaskExecute<string>(_connection, SecurityFacadeQueueName, "SaveNewUser", userToken,
                new object[] { user, userRole });
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            RPCServerTaskExecute(_connection, SecurityFacadeQueueName, "UpdateUser", userToken, new object[] { user, userRole });

        }

        public void RemoveUser(User user, string userToken)
        {
            RPCServerTaskExecute(_connection, SecurityFacadeQueueName, "RemoveUser", userToken, new object[] { user });
        }

        public void Dispose()
        {
            //_commandChannel.Dispose();
            //_connection.Dispose();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }

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

        private void RPCServerTaskExecute(IConnection connection, string commandQueueName, string methodName, string userToken,
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
                        break;
                    }
                }
            }
        }
    }
}
