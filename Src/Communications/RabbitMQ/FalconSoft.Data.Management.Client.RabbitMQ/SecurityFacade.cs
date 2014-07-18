using System;
using System.Collections.Generic;
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

        public SecurityFacade(string hostName)
        {
            var factory = new ConnectionFactory {HostName = hostName};
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            var correlationId = Guid.NewGuid().ToString();
            var queueName = _commandChannel.QueueDeclare().QueueName;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, false, consumer);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            var message = new MethodArgs
            {
                MethodName = "Authenticate",
                UserToken = null,
                MethodsArgs = new object[] {userName, password}
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, props, messageBytes);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<KeyValuePair<bool, string>>(ea.Body);

                    return responce;
                }
            }
        }

        public List<User> GetUsers(string userToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            var queueName = _commandChannel.QueueDeclare().QueueName;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, false, consumer);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            var message = new MethodArgs
            {
                MethodName = "GetUsers",
                UserToken = userToken,
                MethodsArgs = null
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, props, messageBytes);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<List<User>>(ea.Body);

                    return responce;
                }
            }
        }

        public User GetUser(string userName)
        {
            var correlationId = Guid.NewGuid().ToString();
            var queueName = _commandChannel.QueueDeclare().QueueName;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, false, consumer);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            var message = new MethodArgs
            {
                MethodName = "GetUser",
                UserToken = null,
                MethodsArgs = new object[] { userName }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, props, messageBytes);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<User>(ea.Body);

                    return responce;
                }
            }
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            var queueName = _commandChannel.QueueDeclare().QueueName;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, false, consumer);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            var message = new MethodArgs
            {
                MethodName = "SaveNewUser",
                UserToken = userToken,
                MethodsArgs = new object[] { user, userRole }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, props, messageBytes);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<string>(ea.Body);

                    return responce;
                }
            }
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            var message = new MethodArgs
            {
                MethodName = "UpdateUser",
                UserToken = userToken,
                MethodsArgs = new object[] { user, userRole }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, null, messageBytes);
        }

        public void RemoveUser(User user, string userToken)
        {
            var message = new MethodArgs
            {
                MethodName = "RemoveUser",
                UserToken = userToken,
                MethodsArgs = new object[] { user }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SecurityFacadeQueueName, null, messageBytes);
        }

        public void Dispose()
        {
            _connection.Close();
            _commandChannel.Close();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
