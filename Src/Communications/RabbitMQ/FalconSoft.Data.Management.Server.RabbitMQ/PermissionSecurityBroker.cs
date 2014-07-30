using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class PermissionSecurityBroker : IDisposable
    {
        private readonly IPermissionSecurityFacade _permissionSecurityFacade;
        private readonly ILogger _logger;
        private IModel _commandChannel;
        private readonly Task _task;
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";
        private readonly object _establishConnectionLock = new object();

        public PermissionSecurityBroker(string hostName, string userName, string password, IPermissionSecurityFacade permissionSecurityFacade, ILogger logger)
        {
            _permissionSecurityFacade = permissionSecurityFacade;
            _logger = logger;

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
            IConnection connection = factory.CreateConnection();

            _commandChannel = connection.CreateModel();

            _commandChannel.QueueDelete(PermissionSecurityFacadeQueueName);
            _commandChannel.ExchangeDelete(PermissionSecurityFacadeExchangeName);

            _commandChannel.QueueDeclare(PermissionSecurityFacadeQueueName, true, false, false, null);

            _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(PermissionSecurityFacadeQueueName, false, consumer);

            _task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);
                        ExecuteMethodSwitch(message, ea.BasicProperties);
                    }
                    catch (EndOfStreamException ex)
                    {
                        _logger.Debug("PermissionSecurityBroker failed", ex);

                        lock (_establishConnectionLock)
                        {
                            connection = factory.CreateConnection();

                            _commandChannel = connection.CreateModel();

                            _commandChannel.QueueDelete(PermissionSecurityFacadeQueueName);
                            _commandChannel.ExchangeDelete(PermissionSecurityFacadeExchangeName);

                            _commandChannel.QueueDeclare(PermissionSecurityFacadeQueueName, true, false, false, null);
                            _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

                            consumer = new QueueingBasicConsumer(_commandChannel);
                            _commandChannel.BasicConsume(PermissionSecurityFacadeQueueName, false, consumer);
                        }
                    }
                }
            });
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            switch (message.MethodName)
            {
                case "InitializeConnection":
                    {
                        InitializeConnection(basicProperties);
                        break;
                    }
                case "GetUserPermissions":
                    {
                        GetUserPermissions(basicProperties, message.UserToken);
                        break;
                    }
                case "SaveUserPermissions":
                    {
                        SaveUserPermissions(basicProperties, message.UserToken,
                            message.MethodsArgs[0] as Dictionary<string, AccessLevel>, message.MethodsArgs[1] as string);
                        break;
                    }
                case "CheckAccess":
                    {
                        CheckAccess(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetPermissionChanged":
                    {
                        GetPermissionChanged(message.UserToken);
                        break;
                    }
            }
        }

        private void InitializeConnection(IBasicProperties basicProperties)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var corelationId = string.Copy(basicProperties.CorrelationId);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = corelationId;

                    _commandChannel.BasicPublish("", replyTo, props, null);
                }
                catch (Exception ex)
                {
                    _logger.Debug("Failed to responce to client connection confirming.", ex);
                    throw;
                }
            });
        }

        private void GetUserPermissions(IBasicProperties basicProperties, string userToken)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    var data = _permissionSecurityFacade.GetUserPermissions(userToken);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetUserPermissions failed", ex);
                    throw;
                }
            });
        }

        private void SaveUserPermissions(IBasicProperties basicProperties, string userToken, Dictionary<string, AccessLevel> permissions, string targetUserToken)
        {
            try
            {
                var action = new Action<string>(message => RPCSendTaskExecutionResults(basicProperties, message));

                _permissionSecurityFacade.SaveUserPermissions(permissions, targetUserToken, userToken, action);
            }
            catch (Exception ex)
            {
                _logger.Debug("SaveUserPermissions failed", ex);
            }
        }

        private void CheckAccess(IBasicProperties basicProperties, string userToken, string dataSourcePath)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    var data = _permissionSecurityFacade.CheckAccess(userToken, dataSourcePath);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("CheckAccess failed", ex);
                    throw;
                }
            });
        }

        private void GetPermissionChanged(string userToken)
        {
            var severity = string.Copy(userToken);

            _permissionSecurityFacade.GetPermissionChanged(userToken).Subscribe(data =>
            {
                lock (_establishConnectionLock)
                {
                    var dataBytes = BinaryConverter.CastToBytes(data);

                    _commandChannel.BasicPublish(PermissionSecurityFacadeExchangeName, severity, null, dataBytes);
                }
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

        public void Dispose()
        {
            _task.Dispose();
        }
    }
}
