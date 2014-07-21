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
            var factory = new ConnectionFactory {HostName = serverUrl};
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GetRecordsHistory",
                UserToken = null,
                MethodsArgs = new object[] {dataSourceInfo, recordKey}
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var subject = new Subject<Dictionary<string, object>>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var memStream = new MemoryStream();

                        var binForm = new BinaryFormatter();

                        memStream.Write(ea.Body, 0, ea.Body.Length);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var responce = (RabbitMQResponce) binForm.Deserialize(memStream);

                        if (responce.LastMessage) break;

                        var list = (List<Dictionary<string, object>>) responce.Data;

                        foreach (var dictionary in list)
                        {
                            subject.OnNext(dictionary);
                        }
                    }
                }
                subject.OnCompleted();
            });

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GetDataHistoryByTag",
                UserToken = null,
                MethodsArgs = new object[] { dataSourceInfo, tagInfo }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var subject = new Subject<Dictionary<string, object>>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var memStream = new MemoryStream();

                        var binForm = new BinaryFormatter();

                        memStream.Write(ea.Body, 0, ea.Body.Length);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                        if (responce.LastMessage) break;

                        var list = (List<Dictionary<string, object>>)responce.Data;

                        foreach (var dictionary in list)
                        {
                            subject.OnNext(dictionary);
                        }
                    }
                }
                subject.OnCompleted();
            });

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GetRecordsAsOf",
                UserToken = null,
                MethodsArgs = new object[] { dataSourceInfo, timeStamp }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var subject = new Subject<Dictionary<string, object>>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var memStream = new MemoryStream();

                        var binForm = new BinaryFormatter();

                        memStream.Write(ea.Body, 0, ea.Body.Length);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                        if (responce.LastMessage) break;

                        var list = (List<Dictionary<string, object>>)responce.Data;

                        foreach (var dictionary in list)
                        {
                            subject.OnNext(dictionary);
                        }
                    }
                }
                subject.OnCompleted();
            });

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GetTemporalDataByRevisionId",
                UserToken = null,
                MethodsArgs = new[] { dataSourceInfo, revisionId }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var subject = new Subject<Dictionary<string, object>>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var memStream = new MemoryStream();

                        var binForm = new BinaryFormatter();

                        memStream.Write(ea.Body, 0, ea.Body.Length);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                        if (responce.LastMessage) break;

                        var list = (List<Dictionary<string, object>>)responce.Data;

                        foreach (var dictionary in list)
                        {
                            subject.OnNext(dictionary);
                        }
                    }
                }
                subject.OnCompleted();
            });

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            var correlationId = Guid.NewGuid().ToString();

            var replyTo = _commandChannel.QueueDeclare().QueueName;

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = new MethodArgs
            {
                MethodName = "GetRevisions",
                UserToken = null,
                MethodsArgs = new object[] { dataSourceInfo }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, props, messageBytes);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var subject = new Subject<Dictionary<string, object>>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var memStream = new MemoryStream();

                        var binForm = new BinaryFormatter();

                        memStream.Write(ea.Body, 0, ea.Body.Length);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                        if (responce.LastMessage) break;

                        var list = (List<Dictionary<string, object>>)responce.Data;

                        foreach (var dictionary in list)
                        {
                            subject.OnNext(dictionary);
                        }
                    }
                }
                subject.OnCompleted();
            });

            return subject.ToEnumerable();
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
            var message = new MethodArgs
            {
                MethodName = "SaveTagInfo",
                UserToken = null,
                MethodsArgs = new object[] { tagInfo }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, null, messageBytes);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var message = new MethodArgs
            {
                MethodName = "RemoveTagInfo",
                UserToken = null,
                MethodsArgs = new object[] { tagInfo }
            };

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", TemporalDataQueryFacadeQueryName, null, messageBytes);
        }

        public void Dispose()
        {

        }
    }
}