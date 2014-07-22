using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class TemporalDataQueryFacade : ITemporalDataQueryFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string TemporalDataQueryFacadeQueryName = "TemporalDataQueryFacadeRPC";

        public TemporalDataQueryFacade(string serverUrl)
        {
            var factory = new ConnectionFactory { HostName = serverUrl };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, TemporalDataQueryFacadeQueryName,
               "GetRecordsHistory", null, new object[] { dataSourceInfo, recordKey });
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, TemporalDataQueryFacadeQueryName,
               "GetDataHistoryByTag", null, new object[] { dataSourceInfo, tagInfo });
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, TemporalDataQueryFacadeQueryName,
                "GetRecordsAsOf", null, new object[] { dataSourceInfo, timeStamp });
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, TemporalDataQueryFacadeQueryName,
                "GetTemporalDataByRevisionId", null, new[] { dataSourceInfo, revisionId });
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, TemporalDataQueryFacadeQueryName,
                "GetRevisions", null, new object[] { dataSourceInfo });
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GeTagInfos",
                UserToken = null,
                MethodsArgs = null
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var responce = BinaryConverter.CastTo<TagInfo[]>(ea.Body);

                    return responce;
                }
            }
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            RPCServerTaskExecute(_connection, TemporalDataQueryFacadeQueryName, "SaveTagInfo", null,
               new object[] { tagInfo });
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            RPCServerTaskExecute(_connection, TemporalDataQueryFacadeQueryName, "RemoveTagInfo", null,
                new object[] { tagInfo });
        }

        public void Dispose()
        {
            //_commandChannel.Close();
            //_connection.Close();
        }

        private IEnumerable<T> RPCServerTaskExecute<T>(IConnection connection,
           string commandQueueName, string methodName, string userToken, object[] methodArgs)
        {
            var channel = connection.CreateModel();

            var correlationId = Guid.NewGuid().ToString();

            var consumer = new QueueingBasicConsumer(channel);

            var replyTo = channel.QueueDeclare().QueueName;
            channel.BasicConsume(replyTo, true, consumer);

            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = methodName,
                UserToken = userToken,
                MethodsArgs = methodArgs
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            channel.BasicPublish("", commandQueueName, props, messageBytes);

            var subject = new Subject<T>();

            Task.Factory.StartNew(() =>
            {
                var queueName = string.Copy(replyTo);
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var responce = CastTo<RabbitMQResponce>(ea.Body);

                    if (responce.LastMessage)
                    {
                        channel.Dispose();
                        break;
                    }

                    var list = (List<T>)responce.Data;
                    foreach (var dictionary in list)
                    {
                        subject.OnNext(dictionary);
                    }
                }
                _commandChannel.QueueDelete(queueName);

                subject.OnCompleted();
            });
            return subject.ToEnumerable();
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

        private T CastTo<T>(byte[] byteArray)
        {
            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return (T)binForm.Deserialize(memStream);
        }
    }
}