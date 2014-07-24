using System;
using System.Reactive.Disposables;
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
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";

        public MetaDataFacade(string hostName, string userName, string password)
        {
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

            _commandChannel.ExchangeDeclare(MetadataExchangeName, "fanout");

            var queueName = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueName, MetadataExchangeName, "");

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, true, consumer);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var objectInfo = BinaryConverter.CastTo<SourceObjectChangedEventArgs>(ea.Body);

                    if (ObjectInfoChanged != null)
                        ObjectInfoChanged(this, objectInfo);
                }
            });

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            var queueNameForExceptions = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueNameForExceptions, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumerForExceptions);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumerForExceptions.Queue.Dequeue();

                    var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                    if (ErrorMessageHandledAction != null)
                        ErrorMessageHandledAction(array[0], array[1]);
                }
            });
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<DataSourceInfo[]>(_connection, MetadataQueueName, "GetAvailableDataSources",
                userToken, new object[] {minAccessLevel});
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return RPCServerTaskExecute<DataSourceInfo>(_connection, MetadataQueueName, "GetDataSourceInfo", userToken,
                new object[] {dataSourceUrn});
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateDataSourceInfo", userToken,
                new object[] {dataSource, oldDataSourceUrn});
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateDataSourceInfo", userToken,
                new object[] {dataSource});
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteDataSourceInfo", userToken,
                new object[] {dataSourceUrn});
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<WorksheetInfo>(_connection, MetadataQueueName, "GetWorksheetInfo", userToken,
                new object[] {worksheetUrn});
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<WorksheetInfo[]>(_connection, MetadataQueueName, "GetAvailableWorksheets",
                userToken, new object[] {minAccessLevel});
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateWorksheetInfo", userToken,
                new object[] {wsInfo, oldWorksheetUrn});
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateWorksheetInfo", userToken, new object[] {wsInfo});
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteWorksheetInfo", userToken,
                new object[] {worksheetUrn});
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo[]>(_connection, MetadataQueueName,
                "GetAvailableAggregatedWorksheets", userToken, new object[] {minAccessLevel});
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn,
            string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateAggregatedWorksheetInfo", userToken,
                new object[] {wsInfo, oldWorksheetUrn});
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateAggregatedWorksheetInfo", userToken, new object[] { wsInfo });
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteAggregatedWorksheetInfo", userToken,
                new object[] {worksheetUrn});
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo>(_connection, MetadataQueueName,
                "GetAggregatedWorksheetInfo", userToken, new object[] {worksheetUrn});
        }

        public ServerInfo GetServerInfo()
        {
            return RPCServerTaskExecute<ServerInfo>(_connection, MetadataQueueName,"GetServerInfo", null, null);
        }

        public void Dispose()
        {
            //_commandChannel.Dispose();
            //_connection.Dispose();
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public Action<string, string> ErrorMessageHandledAction { get; set; }

        private T RPCServerTaskExecute<T>(IConnection connection, string commandQueueName, string methodName, string userToken,
            object[] methodArgs)
        {
            using (var channel = connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var message = new MethodArgs
                {
                    MethodName = methodName,
                    UserToken = userToken,
                    MethodsArgs = methodArgs
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", commandQueueName, props, messageBytes);

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<T>(ea.Body);
                    }
                }
            }
        }

        private void RPCServerTaskExecute(IConnection connection, string commandQueueName, string methodName, string userToken,
            object[] methodArgs)
        {
            using (var channel = connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var message = new MethodArgs
                {
                    MethodName = methodName,
                    UserToken = userToken,
                    MethodsArgs = methodArgs
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", commandQueueName, props, messageBytes);

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        break;
                    }
                }
            }
        }
    }
}
