using System;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class SearchFacade : ISearchFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string SearchFacadeQueueName = "SearchFacadeRPC";

        public SearchFacade(string serverUrl, string userName, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = serverUrl,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            InitializeConnection(SearchFacadeQueueName);
        }

        public SearchData[] Search(string searchString)
        {
            return RPCServerTaskExecute<SearchData[]>(_connection, SearchFacadeQueueName, "Search", null,
                new object[] {searchString});
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            return RPCServerTaskExecute<HeaderInfo[]>(_connection, SearchFacadeQueueName, "GetSearchableWorksheets",
                null, new object[] {searchData});
        }

        public void Dispose()
        {
            
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
                props.SetPersistent(true);

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
                props.SetPersistent(true);

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

        public void Close()
        {
            _commandChannel.Close();
            _connection.Close();
        }

    }
}