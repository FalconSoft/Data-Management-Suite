using System;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class SearchFacade : ISearchFacade
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
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
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
            //_commandChannel.Close();
            //_connection.Close();
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
    }
}