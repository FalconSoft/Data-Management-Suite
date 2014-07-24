using System;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class MetaDataBroker
    {
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";

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
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            _connection = factory.CreateConnection();

            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDeclare(MetadataQueueName, false, false, false, null);

            _commandChannel.ExchangeDeclare(MetadataExchangeName, "fanout");

            _metaDataAdminFacade.ObjectInfoChanged += OnObjectInfoChanged;

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            _metaDataAdminFacade.ErrorMessageHandledAction = OnErrorMessageHandledAction;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(MetadataQueueName, false, consumer);

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
                case "CreateDataSourceInfo":
                    {
                        CreateDataSourceInfo(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0]);
                        break;
                    }
                case "GetDataSourceInfo":
                    {
                        GetDataSourceInfo(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
                        break;
                    }
                case "GetAvailableDataSources":
                    {
                        GetAvailableDataSources(basicProperties, message.UserToken, (AccessLevel)message.MethodsArgs[0]);
                        break;
                    }
                case "UpdateDataSourceInfo":
                    {
                        UpdateDataSourceInfo(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "DeleteDataSourceInfo":
                    {
                        DeleteDataSourceInfo(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
                        break;
                    }
                case "GetWorksheetInfo":
                    {
                        GetWorksheetInfo(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
                        break;
                    }
                case "GetAvailableWorksheets":
                    {
                        GetAvailableWorksheets(basicProperties, message.UserToken, (AccessLevel)message.MethodsArgs[0]);
                        break;
                    }
                case "UpdateWorksheetInfo":
                    {
                        UpdateWorksheetInfo(basicProperties, message.UserToken, (WorksheetInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "CreateWorksheetInfo":
                    {
                        CreateWorksheetInfo(basicProperties, message.UserToken, (WorksheetInfo)message.MethodsArgs[0]);
                        break;
                    }
                case "DeleteWorksheetInfo":
                    {
                        DeleteWorksheetInfo(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
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
                        UpdateAggregatedWorksheetInfo(basicProperties, (AggregatedWorksheetInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1],
                            message.UserToken);
                        break;
                    }
                case "CreateAggregatedWorksheetInfo":
                    {
                        CreateAggregatedWorksheetInfo(basicProperties, (AggregatedWorksheetInfo)message.MethodsArgs[0], message.UserToken);
                        break;
                    }
                case "DeleteAggregatedWorksheetInfo":
                    {
                        DeleteAggregatedWorksheetInfo(basicProperties, (string)message.MethodsArgs[0], message.UserToken);
                        break;
                    }
                case "GetAggregatedWorksheetInfo":
                    {
                        GetAggregatedWorksheetInfo(basicProperties, (string)message.MethodsArgs[0], message.UserToken);
                        break;
                    }
            }
        }

        private void GetAvailableDataSources(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableDataSources(userToken, accessLevel);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void GetDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourcePath)
        {
            var data = _metaDataAdminFacade.GetDataSourceInfo(dataSourcePath, userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void UpdateDataSourceInfo(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, string oldDataSourcePath)
        {
            _metaDataAdminFacade.UpdateDataSourceInfo(dataSourceInfo, oldDataSourcePath, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void CreateDataSourceInfo(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo)
        {
            _metaDataAdminFacade.CreateDataSourceInfo(dataSourceInfo, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void DeleteDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourthPath)
        {
            _metaDataAdminFacade.DeleteDataSourceInfo(dataSourthPath, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void GetWorksheetInfo(IBasicProperties basicProperties, string userToken, string worksheetUrn)
        {
            var data = _metaDataAdminFacade.GetWorksheetInfo(worksheetUrn, userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void GetAvailableWorksheets(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableWorksheets(userToken, accessLevel);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void UpdateWorksheetInfo(IBasicProperties basicProperties, string userToken, WorksheetInfo worksheetInfo, string oldWorksheetUrn)
        {
            _metaDataAdminFacade.UpdateWorksheetInfo(worksheetInfo, oldWorksheetUrn, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void CreateWorksheetInfo(IBasicProperties basicProperties, string userToken, WorksheetInfo worksheetInfo)
        {
            _metaDataAdminFacade.CreateWorksheetInfo(worksheetInfo, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void DeleteWorksheetInfo(IBasicProperties basicProperties, string userToken, string worksheetUrn)
        {
            _metaDataAdminFacade.DeleteWorksheetInfo(worksheetUrn, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void GetAvailableAggregatedWorksheets(IBasicProperties basicProperties, string userToken,
            AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableAggregatedWorksheets(userToken, accessLevel);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        private void UpdateAggregatedWorksheetInfo(IBasicProperties basicProperties, AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            _metaDataAdminFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void CreateAggregatedWorksheetInfo(IBasicProperties basicProperties, AggregatedWorksheetInfo wsInfo, string userToken)
        {
            _metaDataAdminFacade.CreateAggregatedWorksheetInfo(wsInfo, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void DeleteAggregatedWorksheetInfo(IBasicProperties basicProperties, string worksheetUrn, string userToken)
        {
            _metaDataAdminFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userToken);

            RPCSendTaskExecutionFinishNotification(basicProperties);
        }

        private void GetAggregatedWorksheetInfo(IBasicProperties basicProperties, string worksheetUrn, string userToken)
        {
            var data = _metaDataAdminFacade.GetAggregatedWorksheetInfo(worksheetUrn, userToken);

            RPCSendTaskExecutionResults(basicProperties, data);
        }

        // Pub/Sub notification to all users. Type : fanout
        private void OnObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            var messageBytes = BinaryConverter.CastToBytes(e);
            _commandChannel.BasicPublish(MetadataExchangeName, "", null, messageBytes);
        }

        // Pub/Sub notification to all users. Type : fanout
        private void OnErrorMessageHandledAction(string arg1, string arg2)
        {
            var typle = string.Format("{0}#{1}", arg1, arg2);
            var messageBytes = BinaryConverter.CastToBytes(typle);
            _commandChannel.BasicPublish(ExceptionsExchangeName, "", null, messageBytes);
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
