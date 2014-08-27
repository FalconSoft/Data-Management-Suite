using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class SecurityFacade : ISecurityFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string SecurityFacadeQueueName = "SecurityFacadeRPC";
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private const int TimeOut = 2000;

        public SecurityFacade(string hostName, string userName, string password)
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

            InitializeConnection(SecurityFacadeQueueName);

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            var queueNameForExceptions = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueNameForExceptions, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumerForExceptions);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumerForExceptions.Queue.Dequeue();

                        var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                        if (ErrorMessageHandledAction != null)
                            ErrorMessageHandledAction(array[0], array[1]);
                    }
                    catch (EndOfStreamException ex)
                    {
                        return;
                    }
                }
            }, _cts.Token);
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            return RPCServerTaskExecute<KeyValuePair<bool, string>>(_connection, SecurityFacadeQueueName, "Authenticate",
                null, new object[] { userName, password });
        }

        public List<User> GetUsers(string userToken)
        {
            return RPCServerTaskExecute<List<User>>(_connection, SecurityFacadeQueueName, "GetUsers", userToken, null);
        }

        public User GetUser(string userName)
        {
            return RPCServerTaskExecute<User>(_connection, SecurityFacadeQueueName, "GetUser", null,
                new object[] { userName });
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
                        if (ErrorMessageHandledAction != null)
                            ErrorMessageHandledAction("Connection to server is broken", "Connection to server is broken");

                        return default(T);
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
                        if (ErrorMessageHandledAction != null)
                            ErrorMessageHandledAction("Connection to server is broken", "Connection to server is broken");

                        return;
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
