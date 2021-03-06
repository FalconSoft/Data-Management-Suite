﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private const string PermissionSecurityFacadeQueueName = "PermissionSecurityFacadeRPC";
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";
        private readonly object _establishConnectionLock = new object();
        private volatile bool _keepAlive = true;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private IConnection _connection;
        private readonly Dictionary<string, IDisposable> _getPermissionChangesDisposables = new Dictionary<string, IDisposable>();
        private readonly ConnectionFactory _factory;

        public PermissionSecurityBroker(string hostName, string userName, string password, IPermissionSecurityFacade permissionSecurityFacade, ILogger logger)
        {
            _permissionSecurityFacade = permissionSecurityFacade;
            _logger = logger;

            _factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };
            _connection = _factory.CreateConnection();

            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDelete(PermissionSecurityFacadeQueueName);
            _commandChannel.ExchangeDelete(PermissionSecurityFacadeExchangeName);

            _commandChannel.QueueDeclare(PermissionSecurityFacadeQueueName, true, false, false, null);

            _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(PermissionSecurityFacadeQueueName, false, consumer);

            Task.Factory.StartNew(() =>
            {
                while (_keepAlive)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);
                        ExecuteMethodSwitch(message, ea.BasicProperties);
                    }
                    catch (EndOfStreamException ex)
                    {
                        _logger.Debug(DateTime.Now + " PermissionSecurityBroker failed", ex);

                        lock (_establishConnectionLock)
                        {
                            if (_keepAlive)
                            {
                                _connection = _factory.CreateConnection();

                                _commandChannel = _connection.CreateModel();

                                _commandChannel.QueueDelete(PermissionSecurityFacadeQueueName);
                                _commandChannel.ExchangeDelete(PermissionSecurityFacadeExchangeName);

                                _commandChannel.QueueDeclare(PermissionSecurityFacadeQueueName, true, false, false, null);
                                _commandChannel.ExchangeDeclare(PermissionSecurityFacadeExchangeName, "direct");

                                consumer = new QueueingBasicConsumer(_commandChannel);
                                _commandChannel.BasicConsume(PermissionSecurityFacadeQueueName, false, consumer);
                            }
                        }
                    }
                }
            }, _cts.Token);
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            if (!_keepAlive) return;

            if (message.MethodName != "InitializeConnection")
                _logger.Debug(
                    string.Format(
                        DateTime.Now + " PermissionSecurityBroker. Method Name {0}; User Token {1}; Params {2}",
                        message.MethodName,
                        message.UserToken ?? string.Empty,
                        message.MethodsArgs != null
                            ? message.MethodsArgs.Aggregate("",
                                (cur, next) => cur + " | " + (next != null ? next.ToString() : string.Empty))
                            : string.Empty));

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
                case "Dispose":
                    {
                        Dispose(message.UserToken);
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
            }, _cts.Token);
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
            }, _cts.Token);
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
            }, _cts.Token);
        }

        private void GetPermissionChanged(string userToken)
        {
            if (_getPermissionChangesDisposables.ContainsKey(userToken)) return;

            var disposer = _permissionSecurityFacade.GetPermissionChanged(userToken).Subscribe(data =>
            {
                lock (_establishConnectionLock)
                {
                    var responce = new RabbitMQResponce
                    {
                        Id = 0,
                        Data = data
                    };

                    var dataBytes = BinaryConverter.CastToBytes(responce);

                    try
                    {
                        _commandChannel.BasicPublish(PermissionSecurityFacadeExchangeName, userToken, null, dataBytes);
                    }
                    catch (Exception)
                    {
                        _connection = _factory.CreateConnection();

                        _commandChannel = _connection.CreateModel();
                    }
                }
            });

            _getPermissionChangesDisposables.Add(userToken, disposer);
        }

        private void Dispose(string userToken)
        {
            var routingKey = userToken;

            IDisposable disposable;
            if (_getPermissionChangesDisposables.TryGetValue(routingKey, out disposable))
            {
                disposable.Dispose();
                _getPermissionChangesDisposables.Remove(routingKey);
            }
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
            _keepAlive = false;

            foreach (var permissionChangesDisposable in _getPermissionChangesDisposables)
            {
                permissionChangesDisposable.Value.Dispose();
            }

            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
        }
    }
}
