using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class SearchBroker
    {
        private readonly ISearchFacade _searchFacade;
        private readonly ILogger _logger;
        private IConnection _connection;
        private readonly IModel _commandChannel;
        private const string SearchFacadeQueueName = "SearchFacadeRPC";

        public SearchBroker(string hostName, ISearchFacade searchFacade, ILogger logger)
        {
            _searchFacade = searchFacade;
            _logger = logger;

            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDeclare(SearchFacadeQueueName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(SearchFacadeQueueName, true, consumer);

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
                case "Search":
                    {
                        Search(basicProperties, message.UserToken, (string)message.MethodsArgs[0]);
                        break;
                    }
                case "GetSearchableWorksheets":
                    {
                        GetSearchableWorksheets(basicProperties, message.UserToken, (SearchData)message.MethodsArgs[0]);
                        break;
                    }
            }
        }

        private void Search(IBasicProperties basicProperties, string userToken, string searchString)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            var data = _searchFacade.Search(searchString);

            var dataBytes = BinaryConverter.CastToBytes(data);

            _commandChannel.BasicPublish("", replyTo, props, dataBytes);
        }

        private void GetSearchableWorksheets(IBasicProperties basicProperties, string userToken, SearchData searchData)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            var data = _searchFacade.GetSearchableWorksheets(searchData);

            var dataBytes = BinaryConverter.CastToBytes(data);

            _commandChannel.BasicPublish("", replyTo, props, dataBytes);
        }
    }
}
