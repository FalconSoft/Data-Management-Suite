using System;
using System.Threading.Tasks;
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
        private Task _task;
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

            _task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);
                    ExecuteMethodSwitch(message, ea.BasicProperties);
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
                case "Authenticate":
                    {
                        Authenticate(basicProperties, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string);
                        break;
                    }
                case "GetUsers":
                    {
                        GetUsers(basicProperties, message.UserToken);
                        break;
                    }
                case "GetUser":
                    {
                        GetUser(basicProperties, message.MethodsArgs[0] as string);
                        break;
                    }
                case "SaveNewUser":
                    {
                        SaveNewUser(basicProperties, message.UserToken, message.MethodsArgs[0] as User, (UserRole)message.MethodsArgs[1]);
                        break;
                    }
                case "UpdateUser":
                    {
                        UpdateUser(basicProperties, message.UserToken, message.MethodsArgs[0] as User, (UserRole)message.MethodsArgs[1]);
                        break;
                    }
                case "RemoveUser":
                    {
                        RemoveUser(basicProperties, message.UserToken, message.MethodsArgs[0] as User);
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

        private void Authenticate(IBasicProperties basicProperties, string userName, string password)
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

                  var data = _securityFacade.Authenticate(userName, password);

                  RPCSendTaskExecutionResults(props, data);
              }
              catch (Exception ex)
              {
                  _logger.Debug("Authenticate failed", ex);
                  throw;
              }
          });
        }

        private void GetUsers(IBasicProperties basicProperties, string userToken)
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

                    var data = _securityFacade.GetUsers(userToken);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetUsers failed", ex);
                    throw;
                }
            });
        }

        private void GetUser(IBasicProperties basicProperties, string userName)
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

                    var data = _securityFacade.GetUser(userName);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetUser failed", ex);
                    throw;
                }
            });
        }

        private void SaveNewUser(IBasicProperties basicProperties, string userToken, User user, UserRole userRole)
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

                    var data = _securityFacade.SaveNewUser(user, userRole, userToken);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("SaveNewUser failed", ex);
                    throw;
                }
            });
        }

        private void UpdateUser(IBasicProperties basicProperties, string userToken, User user, UserRole userRole)
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

                    _securityFacade.UpdateUser(user, userRole, userToken);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("SaveNewUser failed", ex);
                    throw;
                }
            });
        }

        private void RemoveUser(IBasicProperties basicProperties, string userToken, User user)
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

                    _securityFacade.RemoveUser(user, userToken);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("SaveNewUser failed", ex);
                    throw;
                }
            });
        }

        private void OnErrorMessageHandledAction(string arg1, string arg2)
        {
            var typle = string.Format("{0}#{1}", arg1, arg2);
            var messageBytes = BinaryConverter.CastToBytes(typle);
            _commandChannel.BasicPublish(ExceptionsExchangeName, "", null, messageBytes);
        }

        public void Dispose()
        {
            _task.Dispose();
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
