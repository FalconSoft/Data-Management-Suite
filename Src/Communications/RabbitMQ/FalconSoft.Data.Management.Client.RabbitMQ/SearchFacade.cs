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

        public SearchFacade(string serverUrl)
        {
            var factory = new ConnectionFactory {HostName = serverUrl};
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public SearchData[] Search(string searchString)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyTo;

            var message = new MethodArgs
            {
                MethodName = "Search",
                UserToken = null,
                MethodsArgs = new object[] {searchString}
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SearchFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<SearchData[]>(ea.Body);

                    return responce;
                }
            }
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyTo;

            var message = new MethodArgs
            {
                MethodName = "GetSearchableWorksheets",
                UserToken = null,
                MethodsArgs = new object[] { searchData }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", SearchFacadeQueueName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                if (correlationId == ea.BasicProperties.CorrelationId)
                {
                    var responce = BinaryConverter.CastTo<HeaderInfo[]>(ea.Body);

                    return responce;
                }
            }
        }

        public void Dispose()
        {
            _commandChannel.Close();
            _connection.Close();
        }
    }
}