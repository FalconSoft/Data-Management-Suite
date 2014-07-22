using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class PermissionSecurityBroker
    {
        private readonly IPermissionSecurityFacade _permissionSecurityFacade;
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";

        public PermissionSecurityBroker(string hostName, IPermissionSecurityFacade permissionSecurityFacade, ILogger logger)
        {
            _permissionSecurityFacade = permissionSecurityFacade;
            _logger = logger;

            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();

            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDeclare(PermissionSecurityFacadeQueueName, false, false, false, null);

            _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(PermissionSecurityFacadeQueueName, false, consumer);

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
                case "GetUserPermissions":
                    {
                        GetUserPermissions(basicProperties, message.UserToken);
                        break;
                    }
                case "SaveUserPermissions":
                    {
                        SaveUserPermissions(basicProperties, message.UserToken,
                            (Dictionary<string, AccessLevel>)message.MethodsArgs[0], (string)message.MethodsArgs[1]);
                        break;
                    }
                case "CheckAccess":
                    {
                        CheckAccess(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
                        break;
                    }
                case "GetPermissionChanged":
                    {
                        GetPermissionChanged(message.UserToken);
                        break;
                    }
            }
        }

        private void GetUserPermissions(IBasicProperties basicProperties, string userToken)
        {
            var data = _permissionSecurityFacade.GetUserPermissions(userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void SaveUserPermissions(IBasicProperties basicProperties, string userToken, Dictionary<string, AccessLevel> permissions, string targetUserToken)
        {
            var action = new Action<string>(message => RPCSendTaskExecutionResults(basicProperties, message));

            _permissionSecurityFacade.SaveUserPermissions(permissions, targetUserToken, userToken, action);
        }

        private void CheckAccess(IBasicProperties basicProperties, string userToken, string dataSourcePath)
        {
            var data = _permissionSecurityFacade.CheckAccess(userToken, dataSourcePath);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void GetPermissionChanged(string userToken)
        {
            _permissionSecurityFacade.GetPermissionChanged(userToken).Subscribe(data =>
            {
                var severity = string.Copy(userToken);

                var dataBytes = BinaryConverter.CastToBytes(data);

                _commandChannel.BasicPublish(PermissionSecurityFacadeExchangeName, severity, null, dataBytes);
            });
        }

        private void RPCSendTaskExecutionResults<T>(IBasicProperties basicProperties, T data)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }
    }
}
