using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class MetaDataFacade : RabbitMQFacadeBase, IMetaDataAdminFacade
    {
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly string _queueName;
        private readonly string _queueNameForExceptions;

        public MetaDataFacade(string hostName, string userName, string password) : base(hostName, userName, password)
        {
            InitializeConnection(MetadataQueueName);

            CommandChannel.ExchangeDeclare(MetadataExchangeName, "fanout");

            _queueName = CommandChannel.QueueDeclare().QueueName;
            CommandChannel.QueueBind(_queueName, MetadataExchangeName, "");

            var consumer = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(_queueName, true, consumer);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var objectInfo = BinaryConverter.CastTo<SourceObjectChangedEventArgs>(ea.Body);

                        if (ObjectInfoChanged != null)
                            ObjectInfoChanged(this, objectInfo);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }, _cts.Token);

            CommandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            _queueNameForExceptions = CommandChannel.QueueDeclare().QueueName;
            CommandChannel.QueueBind(_queueNameForExceptions, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(_queueNameForExceptions, false, consumerForExceptions);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumerForExceptions.Queue.Dequeue();

                        var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                        if (ErrorMessageHandledAction != null)
                            ErrorMessageHandledAction(array[0], array[1]);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            },_cts.Token);
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<DataSourceInfo[]>(Connection, MetadataQueueName, "GetAvailableDataSources",
                userToken, new object[] { minAccessLevel });
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return RPCServerTaskExecute<DataSourceInfo>(Connection, MetadataQueueName, "GetDataSourceInfo", userToken,
                new object[] { dataSourceUrn });
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "UpdateDataSourceInfo", userToken,
                new object[] { dataSource, oldDataSourceUrn });
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "CreateDataSourceInfo", userToken,
                new object[] { dataSource });
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "DeleteDataSourceInfo", userToken,
                new object[] { dataSourceUrn });
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<WorksheetInfo>(Connection, MetadataQueueName, "GetWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<WorksheetInfo[]>(Connection, MetadataQueueName, "GetAvailableWorksheets",
                userToken, new object[] { minAccessLevel });
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "UpdateWorksheetInfo", userToken,
                new object[] { wsInfo, oldWorksheetUrn });
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "CreateWorksheetInfo", userToken, new object[] { wsInfo });
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "DeleteWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo[]>(Connection, MetadataQueueName,
                "GetAvailableAggregatedWorksheets", userToken, new object[] { minAccessLevel });
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn,
            string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "UpdateAggregatedWorksheetInfo", userToken,
                new object[] { wsInfo, oldWorksheetUrn });
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "CreateAggregatedWorksheetInfo", userToken, new object[] { wsInfo });
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(Connection, MetadataQueueName, "DeleteAggregatedWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo>(Connection, MetadataQueueName,
                "GetAggregatedWorksheetInfo", userToken, new object[] { worksheetUrn });
        }

        public ServerInfo GetServerInfo()
        {
            return RPCServerTaskExecute<ServerInfo>(Connection, MetadataQueueName, "GetServerInfo", null, null);
        }

        public void Dispose()
        {
           
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public Action<string, string> ErrorMessageHandledAction { get; set; }

        public new void Close()
        {
            CommandChannel.QueueUnbind(_queueName, MetadataExchangeName, "", null);
            CommandChannel.QueueUnbind(_queueNameForExceptions, ExceptionsExchangeName, "",null);
            _cts.Cancel();
          
            base.Close();
        }
    }
}
