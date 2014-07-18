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

        public MetaDataBroker(string hostName, IMetaDataAdminFacade metaDataAdminFacade, ILogger logger)
        {
            _metaDataAdminFacade = metaDataAdminFacade;
            _logger = logger;
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
            _commandChannel.QueueDeclare(MetadataQueueName, false, false, false, null);

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

        private void GetAvailableAggregatedWorksheets(IBasicProperties basicProperties, string userToken, AccessLevel accessLevel)
        {
            var data = _metaDataAdminFacade.GetAvailableAggregatedWorksheets(userToken, accessLevel);

            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }
    }
}
