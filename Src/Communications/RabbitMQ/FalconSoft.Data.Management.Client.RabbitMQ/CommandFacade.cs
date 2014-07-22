using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class CommandFacade : ICommandFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string CommandFacadeQueueName = "CommandFacadeRPC";

        public CommandFacade(string hostName)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = "test",
                Password = "test",
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };

            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }
        public void SubmitChanges<T>(string dataSourcePath, string userToken, IEnumerable<T> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges(string dataSourcePath, string userToken, IEnumerable<Dictionary<string, object>> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            string toUpdateQueueName = null;
            string toDeleteQueueName = null;

            if (changedRecords != null)
            {
                toUpdateQueueName = Guid.NewGuid().ToString();
                _commandChannel.QueueDeclare(toUpdateQueueName, false, false, false, null);
            }

            if (deleted != null)
            {
                toDeleteQueueName = Guid.NewGuid().ToString();
                _commandChannel.QueueDeclare(toDeleteQueueName, false, false, false, null);
            }

            var message = new MethodArgs
            {
                MethodName = "SubmitChanges",
                UserToken = userToken,
                MethodsArgs = new object[] {dataSourcePath, toUpdateQueueName, toDeleteQueueName}
            };

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var correlationId = Guid.NewGuid().ToString();

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, false, consumer);

            _commandChannel.BasicPublish("", CommandFacadeQueueName, props, BinaryConverter.CastToBytes(message));

            if (changedRecords != null)
            {
                var changeRecordsList = new List<Dictionary<string, object>>();
                var data = new RabbitMQResponce();
                foreach (var changedRecord in changedRecords)
                {
                    changeRecordsList.Add(changedRecord);
                    if (changeRecordsList.Count == 100)
                    {
                        data.Id++;
                        data.Data = changeRecordsList;
                        
                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();
                        
                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        _commandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);
                        
                        changeRecordsList.Clear();
                    }
                }

                if (changeRecordsList.Count > 0)
                {
                    data.Id++;
                    data.Data = changeRecordsList;

                    var bf = new BinaryFormatter();
                    var ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    var messageBytes = ms.ToArray();

                    _commandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);

                    changeRecordsList.Clear();

                    data.LastMessage = true;

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    messageBytes = ms.ToArray();
                    _commandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);
                }
                else
                {
                    data.LastMessage = true;
                    var bf = new BinaryFormatter();
                    var ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    var messageBytes = ms.ToArray();

                    _commandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);
                }
            }

            if (deleted != null)
            {
                var deletedList = new List<string>();

                var data = new RabbitMQResponce();

                foreach (var recordKey in deleted)
                {
                    deletedList.Add(recordKey);
                    if (deletedList.Count == 100)
                    {
                        data.Id++;
                        data.Data = deletedList;

                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        _commandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);

                        deletedList.Clear();
                    }
                }

                if (deletedList.Count > 0)
                {
                    data.Id++;
                    data.Data = deletedList;

                    var bf = new BinaryFormatter();
                    var ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    var messageBytes = ms.ToArray();

                    _commandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);

                    deletedList.Clear();

                    data.LastMessage = true;

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    messageBytes = ms.ToArray();
                    _commandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);
                }
                else
                {
                    data.LastMessage = true;
                    var bf = new BinaryFormatter();
                    var ms = new MemoryStream();

                    bf.Serialize(ms, data);

                    var messageBytes = ms.ToArray();

                    _commandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);
                }
            }

            if (onNotifcation != null)
            {
                onNotifcation("Some text", "Transfer complete");
            }

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var ri = BinaryConverter.CastTo<RevisionInfo>(ea.Body);

                    if (onSuccess != null)
                        onSuccess(ri);

                    break;
                }
            });
        }

        public void Dispose()
        {
            //_commandChannel.Close();
            //_connection.Close();
        }
    }
}
