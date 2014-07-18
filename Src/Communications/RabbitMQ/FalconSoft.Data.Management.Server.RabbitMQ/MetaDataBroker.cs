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
        private IConnection _connection;
        private readonly IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";

        public MetaDataBroker(string hostName, IMetaDataAdminFacade metaDataAdminFacade, ILogger logger)
        {
            _metaDataAdminFacade = metaDataAdminFacade;
            _logger = logger;

            var factory = new ConnectionFactory { HostName = hostName };
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
                        CreateDataSourceInfo(message.UserToken, (DataSourceInfo)message.MethodsArgs[0]);
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
                        UpdateDataSourceInfo(message.UserToken, (DataSourceInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "DeleteDataSourceInfo":
                    {
                        DeleteDataSourceInfo(message.UserToken, (string)message.MethodsArgs[0]);
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
                        UpdateWorksheetInfo(message.UserToken, (WorksheetInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "CreateWorksheetInfo":
                    {
                        CreateWorksheetInfo(message.UserToken, (WorksheetInfo)message.MethodsArgs[0]);
                        break;
                    }
                case "DeleteWorksheetInfo":
                    {
                        DeleteWorksheetInfo(message.UserToken, (string)message.MethodsArgs[0]);
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
                        UpdateAggregatedWorksheetInfo((AggregatedWorksheetInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1],
                            message.UserToken);
                        break;
                    }
                case "CreateAggregatedWorksheetInfo":
                    {
                        CreateAggregatedWorksheetInfo((AggregatedWorksheetInfo)message.MethodsArgs[0], message.UserToken);
                        break;
                    }
                case "DeleteAggregatedWorksheetInfo":
                    {
                        DeleteAggregatedWorksheetInfo((string)message.MethodsArgs[0], message.UserToken);
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

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void GetDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourcePath)
        {
            var data = _metaDataAdminFacade.GetDataSourceInfo(dataSourcePath, userToken);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void UpdateDataSourceInfo(string userToken, DataSourceInfo dataSourceInfo, string oldDataSourcePath)
        {
            _metaDataAdminFacade.UpdateDataSourceInfo(dataSourceInfo, oldDataSourcePath, userToken);
        }

        private void CreateDataSourceInfo(string userToken, DataSourceInfo dataSourceInfo)
        {
            _metaDataAdminFacade.CreateDataSourceInfo(dataSourceInfo, userToken);
        }

        private void DeleteDataSourceInfo(string userToken, string dataSourthPath)
        {
            _metaDataAdminFacade.DeleteDataSourceInfo(dataSourthPath, userToken);
        }

        private void GetWorksheetInfo(IBasicProperties basicProperties, string userToken, string worksheetUrn)
        {
            var data = _metaDataAdminFacade.GetWorksheetInfo(worksheetUrn, userToken);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void GetAvailableWorksheets(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableWorksheets(userToken, accessLevel);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        private void UpdateWorksheetInfo(string userToken, WorksheetInfo worksheetInfo, string oldWorksheetUrn)
        {
            _metaDataAdminFacade.UpdateWorksheetInfo(worksheetInfo, oldWorksheetUrn, userToken);
        }

        private void CreateWorksheetInfo(string userToken, WorksheetInfo worksheetInfo)
        {
            _metaDataAdminFacade.CreateWorksheetInfo(worksheetInfo, userToken);
        }

        private void DeleteWorksheetInfo(string userToken, string worksheetUrn)
        {
            _metaDataAdminFacade.DeleteWorksheetInfo(worksheetUrn, userToken);
        }

        private void GetAvailableAggregatedWorksheets(IBasicProperties basicProperties, string userToken,
            AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableAggregatedWorksheets(userToken, accessLevel);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
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
            var typle = Tuple.Create(arg1, arg2);
            var messageBytes = BinaryConverter.CastToBytes(typle);
            _commandChannel.BasicPublish(MetadataExchangeName, "", null, messageBytes);
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            _metaDataAdminFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            _metaDataAdminFacade.CreateAggregatedWorksheetInfo(wsInfo, userToken);
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            _metaDataAdminFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userToken);
        }

        public void GetAggregatedWorksheetInfo(IBasicProperties basicProperties, string worksheetUrn, string userToken)
        {
            var data = _metaDataAdminFacade.GetAggregatedWorksheetInfo(worksheetUrn, userToken);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }
    }

}
