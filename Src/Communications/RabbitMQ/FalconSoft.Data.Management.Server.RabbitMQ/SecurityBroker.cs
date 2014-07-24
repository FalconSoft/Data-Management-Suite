using System;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class SecurityBroker : IDisposable
    {
        private readonly ISecurityFacade _securityFacade;
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string SecurityFacadeQueueName = "SecurityFacadeRPC";
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";

        public SecurityBroker(string hostName, string userName, string password, ISecurityFacade securityFacade, ILogger logger)
        {
            _securityFacade = securityFacade;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            _securityFacade.ErrorMessageHandledAction = OnErrorMessageHandledAction;

            _commandChannel.QueueDeclare(SecurityFacadeQueueName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(SecurityFacadeQueueName, false, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);
                ExecuteMethodSwitch(message, ea.BasicProperties);
            }
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            switch (message.MethodName)
            {
                case "Authenticate":
                {
                    Authenticate(basicProperties, (string) message.MethodsArgs[0], (string) message.MethodsArgs[1]);
                    break;
                }
                case "GetUsers":
                {
                    GetUsers(basicProperties, message.UserToken);
                    break;
                }
                case "GetUser":
                {
                    GetUser(basicProperties, (string)message.MethodsArgs[0]);
                    break;
                }
                case "SaveNewUser":
                {
                    SaveNewUser(basicProperties, message.UserToken, (User)message.MethodsArgs[0], (UserRole)message.MethodsArgs[1]);
                    break;
                }
                case "UpdateUser":
                {
                    UpdateUser(basicProperties, message.UserToken, (User)message.MethodsArgs[0], (UserRole)message.MethodsArgs[1]);
                    break;
                }
                case "RemoveUser":
                {
                    RemoveUser(basicProperties, message.UserToken, (User)message.MethodsArgs[0]);
                    break;
                }
            }
        }

        private void Authenticate(IBasicProperties basicProperties, string userName, string password)
        {
            var data = _securityFacade.Authenticate(userName, password);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void GetUsers(IBasicProperties basicProperties, string userToken)
        {
            var data = _securityFacade.GetUsers(userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void GetUser(IBasicProperties basicProperties, string userName)
        {
            var data = _securityFacade.GetUser(userName);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void SaveNewUser(IBasicProperties basicProperties, string userToken, User user, UserRole userRole)
        {
            var data = _securityFacade.SaveNewUser(user, userRole, userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void UpdateUser(IBasicProperties basicProperties, string userToken, User user, UserRole userRole)
        {
            _securityFacade.UpdateUser(user, userRole, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void RemoveUser(IBasicProperties basicProperties, string userToken, User user)
        {
            _securityFacade.RemoveUser(user, userToken);

           RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void OnErrorMessageHandledAction(string arg1, string arg2)
        {
            var typle = string.Format("{0}#{1}", arg1, arg2);
            var messageBytes = BinaryConverter.CastToBytes(typle);
            _commandChannel.BasicPublish(ExceptionsExchangeName, "", null, messageBytes);
        }

        public void Dispose()
        {
            _commandChannel.Close();
            _connection.Close();
        }

        private void RPCSendTaskExecutionResults<T>(IBasicProperties basicProperties, T data)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void RPCSendTaskExecutionFinishNotification(IBasicProperties basicProperties)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, null);
        }
    }
}
