using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class MetaDataFacade : IMetaDataAdminFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string MetadataQueueName = "MetaDataFacadeRPC";
        private const string MetadataExchangeName = "MetaDataFacadeExchange";
        private const string ExceptionsExchangeName = "MetaDataFacadeExceptionsExchangeName";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

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

            _commandChannel.ExchangeDeclare(ExceptionsExchangeName, "fanout");

            var queueNameForExceptions = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueNameForExceptions, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumerForExceptions);

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

            InitializeConnection(MetadataQueueName);
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<DataSourceInfo[]>(_connection, MetadataQueueName, "GetAvailableDataSources",
                userToken, new object[] { minAccessLevel });
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return RPCServerTaskExecute<DataSourceInfo>(_connection, MetadataQueueName, "GetDataSourceInfo", userToken,
                new object[] { dataSourceUrn });
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateDataSourceInfo", userToken,
                new object[] { dataSource, oldDataSourceUrn });
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateDataSourceInfo", userToken,
                new object[] { dataSource });
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteDataSourceInfo", userToken,
                new object[] { dataSourceUrn });
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<WorksheetInfo>(_connection, MetadataQueueName, "GetWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<WorksheetInfo[]>(_connection, MetadataQueueName, "GetAvailableWorksheets",
                userToken, new object[] { minAccessLevel });
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateWorksheetInfo", userToken,
                new object[] { wsInfo, oldWorksheetUrn });
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateWorksheetInfo", userToken, new object[] { wsInfo });
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo[]>(_connection, MetadataQueueName,
                "GetAvailableAggregatedWorksheets", userToken, new object[] { minAccessLevel });
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn,
            string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "UpdateAggregatedWorksheetInfo", userToken,
                new object[] { wsInfo, oldWorksheetUrn });
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "CreateAggregatedWorksheetInfo", userToken, new object[] { wsInfo });
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            RPCServerTaskExecute(_connection, MetadataQueueName, "DeleteAggregatedWorksheetInfo", userToken,
                new object[] { worksheetUrn });
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            return RPCServerTaskExecute<AggregatedWorksheetInfo>(_connection, MetadataQueueName,
                "GetAggregatedWorksheetInfo", userToken, new object[] { worksheetUrn });
        }

        public ServerInfo GetServerInfo()
        {
            return RPCServerTaskExecute<ServerInfo>(_connection, MetadataQueueName, "GetServerInfo", null, null);
        }

        public void Dispose()
        {
           
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public Action<string, string> ErrorMessageHandledAction { get; set; }

        public void Close()
        {
            _cts.Cancel();
            _commandChannel.Close();
            _connection.Close();
        }

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

        private void InitializeConnection(string commandQueueName)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "InitializeConnection",
                    UserToken = null,
                    MethodsArgs = null
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                var replyTo = channel.QueueDeclare().QueueName;

                var correlationId = Guid.NewGuid().ToString();

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyTo;

                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(replyTo, true, consumer);

                channel.BasicPublish("", commandQueueName, props, messageBytes);

                while (true)
                {
                    BasicDeliverEventArgs ea;
                    if (consumer.Queue.Dequeue(30000, out ea))
                    {
                        if (correlationId == ea.BasicProperties.CorrelationId)
                        {
                            return;
                        }
                    }
                    throw new Exception("Connection to server failed");
                }
            }
        }
    }
}
