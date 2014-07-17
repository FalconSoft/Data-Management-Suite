using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
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
            var factory = new ConnectionFactory {HostName = hostName};
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
            }
        }

        private void GetDataSourceInfo(IBasicProperties basicProperties, string userToken, string dataSourcePath)
        {
            var correlationId = basicProperties.CorrelationId;
            var replyTo = basicProperties.ReplyTo;
            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            var data = _metaDataAdminFacade.GetDataSourceInfo(dataSourcePath, userToken);
            _commandChannel.BasicPublish("",replyTo,props,BinaryConverter.CastToBytes(data));
        }

        private void CreateDataSourceInfo(string userToken, DataSourceInfo dataSourceInfo)
        {
           _metaDataAdminFacade.CreateDataSourceInfo(dataSourceInfo, userToken);
        }
    }
}
