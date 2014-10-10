using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class MetaDataBroker : IDisposable
    {
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ILogger _logger;
        private IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";
        private readonly object _establishConnectionLock = new object();
        private volatile bool _keepAlive = true;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private IConnection _connection;

        public MetaDataBroker(string hostName, string userName, string password, IMetaDataAdminFacade metaDataAdminFacade, ILogger logger)
        {
            _metaDataAdminFacade = metaDataAdminFacade;
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
            _connection = factory.CreateConnection();

            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDelete(MetadataQueueName);
            _commandChannel.ExchangeDelete(MetadataExchangeName);
            _commandChannel.ExchangeDelete(ExceptionsExchangeName);

            _commandChannel.QueueDeclare(MetadataQueueName, true, false, false, null);

            _commandChannel.ExchangeDeclare(MetadataExchangeName, "fanout");

            _metaDataAdminFacade.ObjectInfoChanged += OnObjectInfoChanged;

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            _metaDataAdminFacade.ErrorMessageHandledAction = OnErrorMessageHandledAction;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(MetadataQueueName, false, consumer);

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
                        _logger.Debug(DateTime.Now + " MetaDataBroker failed", ex);

                        lock (_establishConnectionLock)
                        {
                            if (_keepAlive)
                            {
                                _connection = factory.CreateConnection();

                                _commandChannel = _connection.CreateModel();

                                _commandChannel.QueueDelete(MetadataQueueName);
                                _commandChannel.ExchangeDelete(MetadataExchangeName);
                                _commandChannel.ExchangeDelete(ExceptionsExchangeName);

                                _commandChannel.QueueDeclare(MetadataQueueName, true, false, false, null);
                                _commandChannel.ExchangeDeclare(MetadataExchangeName, "fanout");
                                _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

                                consumer = new QueueingBasicConsumer(_commandChannel);
                                _commandChannel.BasicConsume(MetadataQueueName, false, consumer);
                            }
                        }
                    }

                    catch (NullReferenceException ex)
                    {
                        logger.Debug("MetaDataBroker failed due to wrong parameters", ex);
                    }
                }
            }, _cts.Token);
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            if (!_keepAlive) return;
            if (message.MethodName != "InitializeConnection")
                _logger.Debug(
                    string.Format(DateTime.Now + " MetaDataBroker. Method Name {0}; User Token {1}; Params {2}",
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
                case "CreateDataSourceInfo":
                    {
                        CreateDataSourceInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo);
                        break;
                    }
                case "GetDataSourceInfo":
                    {
                        GetDataSourceInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetAvailableDataSources":
                    {
                        GetAvailableDataSources(basicProperties, message.UserToken, (AccessLevel)message.MethodsArgs[0]);
                        break;
                    }
                case "UpdateDataSourceInfo":
                    {
                        UpdateDataSourceInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo,
                            message.MethodsArgs[1] as string);
                        break;
                    }
                case "DeleteDataSourceInfo":
                    {
                        DeleteDataSourceInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetWorksheetInfo":
                    {
                        GetWorksheetInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetAvailableWorksheets":
                    {
                        GetAvailableWorksheets(basicProperties, message.UserToken, (AccessLevel)message.MethodsArgs[0]);
                        break;
                    }
                case "UpdateWorksheetInfo":
                    {
                        UpdateWorksheetInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as WorksheetInfo,
                            message.MethodsArgs[1] as string);
                        break;
                    }
                case "CreateWorksheetInfo":
                    {
                        CreateWorksheetInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as WorksheetInfo);
                        break;
                    }
                case "DeleteWorksheetInfo":
                    {
                        DeleteWorksheetInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetAvailableAggregatedWorksheets":
                    {
                        GetAvailableAggregatedWorksheets(basicProperties, message.UserToken,
                            (AccessLevel)message.MethodsArgs[0]);
                        break;
                    }
                case "UpdateAggregatedWorksheetInfo":
                    {
                        UpdateAggregatedWorksheetInfo(basicProperties, message.MethodsArgs[0] as AggregatedWorksheetInfo,
                            message.MethodsArgs[1] as string, message.UserToken);
                        break;
                    }
                case "CreateAggregatedWorksheetInfo":
                    {
                        CreateAggregatedWorksheetInfo(basicProperties, message.MethodsArgs[0] as AggregatedWorksheetInfo, message.UserToken);
                        break;
                    }
                case "DeleteAggregatedWorksheetInfo":
                    {
                        DeleteAggregatedWorksheetInfo(basicProperties, message.MethodsArgs[0] as string, message.UserToken);
                        break;
                    }
                case "GetAggregatedWorksheetInfo":
                    {
                        GetAggregatedWorksheetInfo(basicProperties, message.MethodsArgs[0] as string, message.UserToken);
                        break;
                    }
                case "GetServerInfo":
                    {
                        GetServerInfo(basicProperties);
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

        private void GetAvailableDataSources(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
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

                    var data = _metaDataAdminFacade.GetAvailableDataSources(userToken, accessLevel);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetAvailableDataSources failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void GetDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourcePath)
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

                    var data = _metaDataAdminFacade.GetDataSourceInfo(dataSourcePath, userToken);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetDataSourceInfo failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void UpdateDataSourceInfo(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, string oldDataSourcePath)
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

                   _metaDataAdminFacade.UpdateDataSourceInfo(dataSourceInfo, oldDataSourcePath, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("UpdateDataSourceInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void CreateDataSourceInfo(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo)
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

                   _metaDataAdminFacade.CreateDataSourceInfo(dataSourceInfo, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("CreateDataSourceInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void DeleteDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourthPath)
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

                   _metaDataAdminFacade.DeleteDataSourceInfo(dataSourthPath, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("DeleteDataSourceInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void GetWorksheetInfo(IBasicProperties basicProperties, string userToken, string worksheetUrn)
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

                   var data = _metaDataAdminFacade.GetWorksheetInfo(worksheetUrn, userToken);

                   RPCSendTaskExecutionResults(props, data);
               }
               catch (Exception ex)
               {
                   _logger.Debug("GetWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void GetAvailableWorksheets(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
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

                   var data = _metaDataAdminFacade.GetAvailableWorksheets(userToken, accessLevel);

                   RPCSendTaskExecutionResults(props, data);
               }
               catch (Exception ex)
               {
                   _logger.Debug("GetAvailableWorksheets failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void UpdateWorksheetInfo(IBasicProperties basicProperties, string userToken, WorksheetInfo worksheetInfo, string oldWorksheetUrn)
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

                   _metaDataAdminFacade.UpdateWorksheetInfo(worksheetInfo, oldWorksheetUrn, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("UpdateWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void CreateWorksheetInfo(IBasicProperties basicProperties, string userToken, WorksheetInfo worksheetInfo)
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

                    _metaDataAdminFacade.CreateWorksheetInfo(worksheetInfo, userToken);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("CreateWorksheetInfo failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void DeleteWorksheetInfo(IBasicProperties basicProperties, string userToken, string worksheetUrn)
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

                    _metaDataAdminFacade.DeleteWorksheetInfo(worksheetUrn, userToken);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("CreateWorksheetInfo failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void GetAvailableAggregatedWorksheets(IBasicProperties basicProperties, string userToken,
            AccessLevel accessLevel)
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

                    var data = _metaDataAdminFacade.GetAvailableAggregatedWorksheets(userToken, accessLevel);

                    RPCSendTaskExecutionResults(props, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetAvailableAggregatedWorksheets failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void UpdateAggregatedWorksheetInfo(IBasicProperties basicProperties, AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
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

                   _metaDataAdminFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("UpdateAggregatedWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void CreateAggregatedWorksheetInfo(IBasicProperties basicProperties, AggregatedWorksheetInfo wsInfo, string userToken)
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

                   _metaDataAdminFacade.CreateAggregatedWorksheetInfo(wsInfo, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("CreateAggregatedWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void DeleteAggregatedWorksheetInfo(IBasicProperties basicProperties, string worksheetUrn, string userToken)
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

                   _metaDataAdminFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userToken);

                   RPCSendTaskExecutionFinishNotification(props);
               }
               catch (Exception ex)
               {
                   _logger.Debug("DeleteAggregatedWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void GetAggregatedWorksheetInfo(IBasicProperties basicProperties, string worksheetUrn, string userToken)
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

                   var data = _metaDataAdminFacade.GetAggregatedWorksheetInfo(worksheetUrn, userToken);

                   RPCSendTaskExecutionResults(props, data);
               }
               catch (Exception ex)
               {
                   _logger.Debug("GetAggregatedWorksheetInfo failed", ex);
                   throw;
               }
           }, _cts.Token);
        }

        private void GetServerInfo(IBasicProperties basicProperties)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);
                    var corelationId = string.Copy(basicProperties.CorrelationId);

                    var props = _commandChannel.CreateBasicProperties();

                    props.CorrelationId = corelationId;

                    var data = _metaDataAdminFacade.GetServerInfo();

                    _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetServerInfo failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        // Pub/Sub notification to all users. Type : fanout
        private void OnObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            lock (_establishConnectionLock)
            {
                var messageBytes = BinaryConverter.CastToBytes(e);
                _commandChannel.BasicPublish(MetadataExchangeName, "", null, messageBytes);
            }
        }

        // Pub/Sub notification to all users. Type : fanout
        private void OnErrorMessageHandledAction(string arg1, string arg2)
        {
            lock (_establishConnectionLock)
            {
                var typle = string.Format("{0}#{1}", arg1, arg2);
                var messageBytes = BinaryConverter.CastToBytes(typle);
                _commandChannel.BasicPublish(ExceptionsExchangeName, "", null, messageBytes);
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

        private void RPCSendTaskExecutionFinishNotification(IBasicProperties basicProperties)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, null);
        }

        public void Dispose()
        {
            _keepAlive = false;

            _metaDataAdminFacade.ObjectInfoChanged -= OnObjectInfoChanged;

            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
        }
    }
}
