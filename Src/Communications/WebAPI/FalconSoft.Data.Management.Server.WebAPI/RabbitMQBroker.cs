using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.WebAPI
{
    internal class RabbitMQBroker : IRabbitMQBroker
    {
        private IConnection _connection;
        private IModel _commandChannel;
        private readonly ConnectionFactory _factory;
        private const string CommandQueueName = "CommandQueue";

        public RabbitMQBroker(string hostName, string userName, string password, string virtualHost)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            CreateConnection();

            _commandChannel.QueueDelete(CommandQueueName);

            _commandChannel.QueueDeclare(CommandQueueName, true, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(CommandQueueName, false, consumer);

            new Task(() =>
            {
                while (true)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();

                        var props = ea.BasicProperties;

                        var replyTo = props.ReplyTo;

                        _commandChannel.BasicPublish("", replyTo, null, null);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }).Start();
        }

        private void CreateConnection()
        {
            _connection = _factory.CreateConnection();

            _commandChannel = _connection.CreateModel();
        }


        public void CreateExchange(string name, string type)
        {
            _commandChannel.ExchangeDeclare(name, type);
        }


        public void SendMessage(byte[] byteArray, string exchangeName, string exchangeType, string routingKey, string correlationId = null,
            string replyTo = null)
        {
            IBasicProperties basicProperties = null;
            if (correlationId != null || replyTo != null)
            {
                basicProperties = _commandChannel.CreateBasicProperties();
                basicProperties.CorrelationId = correlationId;
                basicProperties.ReplyTo = replyTo;
            }

            if (_commandChannel.IsOpen)
            {
                _commandChannel.BasicPublish(exchangeName, routingKey, basicProperties, byteArray);
            }
            else
            {
                CreateConnection();

                _commandChannel.ExchangeDeclare(exchangeName, "fanout");
                _commandChannel.BasicPublish(exchangeName, routingKey, basicProperties, byteArray);
            }
        }

        public byte[] CastToBytes(object obj)
        {
            if (obj == null)
                return null;

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            _commandChannel.Close();
            _connection.Close();
        }
    }

    public interface IRabbitMQBroker : IDisposable
    {
        void CreateExchange(string name, string type);

        void SendMessage(byte[] byteArray, string exchangeName, string exchangeType, string routingKey, string correlationId = null,
            string replyTo = null);

        byte[] CastToBytes(object obj);
    }
}
