using System;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class SearchBroker : IDisposable
    {
        private readonly ISearchFacade _searchFacade;
        private readonly ILogger _logger;
        private IConnection _connection;
        private readonly IModel _commandChannel;
        private Task _task;
        private const string SearchFacadeQueueName = "SearchFacadeRPC";

        public SearchBroker(string hostName, string userName, string password, ISearchFacade searchFacade, ILogger logger)
        {
            _searchFacade = searchFacade;
            _logger = logger;

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

            _commandChannel.QueueDeclare(SearchFacadeQueueName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(SearchFacadeQueueName, true, consumer);

            _task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);
                    ExecuteMethodSwitch(message, ea.BasicProperties);
                }
            });
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            switch (message.MethodName)
            {
                case "InitializeConnection":
                    {
                        InitializeConnection(basicProperties);
                        break;
                    }
                case "Search":
                    {
                        Search(basicProperties, message.UserToken, message.MethodsArgs[0] as string);
                        break;
                    }
                case "GetSearchableWorksheets":
                    {
                        GetSearchableWorksheets(basicProperties, message.UserToken, message.MethodsArgs[0] as SearchData);
                        break;
                    }
            }
        }

        private void InitializeConnection(IBasicProperties basicProperties)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var corelationId = string.Copy(basicProperties.CorrelationId);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = corelationId;

                    _commandChannel.BasicPublish("", replyTo, props, null);
                }
                catch (Exception ex)
                {
                    _logger.Debug("Failed to responce to client connection confirming.", ex);
                    throw;
                }
            });
        }

        private void Search(IBasicProperties basicProperties, string userToken, string searchString)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    var data = _searchFacade.Search(searchString);

                    RPCSendTaskExecutionResults(basicProperties, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("Search failed", ex);
                    throw;
                }
            });
        }

        private void GetSearchableWorksheets(IBasicProperties basicProperties, string userToken, SearchData searchData)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    var data = _searchFacade.GetSearchableWorksheets(searchData);

                    RPCSendTaskExecutionResults(basicProperties, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetSearchableWorksheets failed", ex);
                    throw;
                }
            });
        }

        private void RPCSendTaskExecutionResults<T>(IBasicProperties basicProperties, T data)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(data));
        }

        public void Dispose()
        {
            _task.Dispose();
        }
    }
}
