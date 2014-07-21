using System;
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

        public MetaDataFacade(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
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
            _commandChannel.QueueBind(queueName, ExceptionsExchangeName, "");

            var consumerForExceptions = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueNameForExceptions, false, consumer);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumerForExceptions.Queue.Dequeue();

                    var array = BinaryConverter.CastTo<string>(ea.Body).Split('#');

                    if (ObjectInfoChanged != null)
                        ErrorMessageHandledAction(array[0], array[1]);
                }
            });
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetAvailableDataSources",
                    UserToken = userToken,
                    MethodsArgs = new object[] {minAccessLevel}
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<DataSourceInfo[]>(ea.Body);
                    }
                }
            }
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetDataSourceInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {dataSourceUrn}
                };
                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<DataSourceInfo>(ea.Body);
                    }
                }
            }
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
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
                    MethodName = "UpdateDataSourceInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {dataSource, oldDataSourceUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, props, messageBytes);

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

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            using (var channel = _connection.CreateModel())
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
                    MethodName = "CreateDataSourceInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {dataSource}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, props, messageBytes);

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

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "DeleteDataSourceInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {dataSourceUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {worksheetUrn}
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<WorksheetInfo>(ea.Body);
                    }
                }
            }
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetAvailableWorksheets",
                    UserToken = userToken,
                    MethodsArgs = new object[] {minAccessLevel}
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<WorksheetInfo[]>(ea.Body);
                    }
                }
            }
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "UpdateWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {wsInfo, oldWorksheetUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            using (var channel = _connection.CreateModel())
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
                    MethodName = "CreateWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {wsInfo}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, props, messageBytes);

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

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "DeleteWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {worksheetUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetAvailableAggregatedWorksheets",
                    UserToken = userToken,
                    MethodsArgs = new object[] {minAccessLevel}
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<AggregatedWorksheetInfo[]>(ea.Body);
                    }
                }
            }
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn,
            string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "UpdateAggregatedWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {wsInfo, oldWorksheetUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "CreateAggregatedWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {wsInfo}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var message = new MethodArgs
                {
                    MethodName = "DeleteAggregatedWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {worksheetUrn}
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", MetadataQueueName, null, messageBytes);
            }
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetAggregatedWorksheetInfo",
                    UserToken = userToken,
                    MethodsArgs = new object[] {worksheetUrn}
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<AggregatedWorksheetInfo>(ea.Body);
                    }
                }
            }
        }

        public ServerInfo GetServerInfo()
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var queueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = queueName;

                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(queueName, false, consumer);

                var methodArgs = new MethodArgs
                {
                    MethodName = "GetServerInfo",
                    UserToken = null,
                    MethodsArgs = null
                };

                channel.BasicPublish("", MetadataQueueName, props, BinaryConverter.CastToBytes(methodArgs));

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        channel.QueueDelete(queueName);
                        return BinaryConverter.CastTo<ServerInfo>(ea.Body);
                    }
                }
            }
        }

        public void Dispose()
        {
            _commandChannel.Close();
            _connection.Close();
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
