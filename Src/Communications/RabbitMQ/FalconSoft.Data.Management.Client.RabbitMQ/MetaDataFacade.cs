using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class MetaDataFacade : IMetaDataAdminFacade
    {
        private IConnection _connection;
        private IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";

        public MetaDataFacade(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            throw new NotImplementedException();
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            var queueName = _commandChannel.QueueDeclare().QueueName;
            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = queueName;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, false, consumer);

            var methodArgs = new MethodArgs
            {
                MethodName = "GetDataSourceInfo",
                UserToken = userToken,
                MethodsArgs = new object[] { dataSourceUrn }
            };
            _commandChannel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));


            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == correlationId)
                    _commandChannel.QueueDelete(queueName);
                return BinaryConverter.CastTo<DataSourceInfo>(ea.Body);
            }
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            throw new NotImplementedException();
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            throw new NotImplementedException();
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public ServerInfo GetServerInfo()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
