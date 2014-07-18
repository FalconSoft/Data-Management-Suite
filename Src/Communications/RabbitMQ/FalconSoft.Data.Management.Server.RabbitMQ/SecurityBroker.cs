using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public SecurityBroker(string hostName, ISecurityFacade securityFacade, ILogger logger)
        {
            _securityFacade = securityFacade;
            _logger = logger;

            var factory = new ConnectionFactory {HostName = hostName};
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDeclare(SecurityFacadeQueueName, false, false, false, null);

            var consummer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(SecurityFacadeQueueName, false, consummer);

            while (true)
            {
                var ea = consummer.Queue.Dequeue();

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
                    UpdateUser(message.UserToken, (User)message.MethodsArgs[0], (UserRole)message.MethodsArgs[1]);
                    break;
                }
                case "RemoveUser":
                {
                    RemoveUser(message.UserToken, (User)message.MethodsArgs[0]);
                    break;
                }
            }
        }

        private void Authenticate(IBasicProperties basicProperties, string userName, string password)
        {
            var data = _securityFacade.Authenticate(userName, password);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            _commandChannel.BasicPublish("",replyTo,props,BinaryConverter.CastToBytes(data));
        }

        private void GetUsers(IBasicProperties basicProperties, string userToken)
        {
            var data = _securityFacade.GetUsers(userToken);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void GetUser(IBasicProperties basicProperties, string userName)
        {
            var data = _securityFacade.GetUser(userName);
            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void SaveNewUser(IBasicProperties basicProperties, string userToken, User user, UserRole userRole)
        {
            var data = _securityFacade.SaveNewUser(user, userRole, userToken);

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void UpdateUser(string userToken, User user, UserRole userRole)
        {
            _securityFacade.UpdateUser(user, userRole, userToken);
        }

        private void RemoveUser(string userToken, User user)
        {
            _securityFacade.RemoveUser(user, userToken);
        }

        public void Dispose()
        {
            _commandChannel.Close();
            _connection.Close();
        }
    }
}
