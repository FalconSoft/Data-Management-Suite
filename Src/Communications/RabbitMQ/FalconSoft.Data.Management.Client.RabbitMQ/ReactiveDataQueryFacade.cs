using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class ReactiveDataQueryFacade : IReactiveDataQueryFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        public ReactiveDataQueryFacade(string hostName)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = "RWClient",
                Password = "RWClient",
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
             FilterRule[] filterRules = null)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, RPCQueryName, "GetAggregatedData", userToken,
                new object[] { dataSourcePath, aggregatedWorksheet, filterRules });
        }

        public IEnumerable<T> GetData<T>(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetData(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, RPCQueryName, "GetData", userToken,
                new object[] { dataSourcePath, filterRules });
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            _commandChannel.ExchangeDeclare("GetDataChangesTopic", "topic");

            var replyTo = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(replyTo, "GetDataChangesTopic", dataSourcePath + "." + userToken);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var message = MethdoArgsToByte("GetDataChanges", userToken, new object[] { dataSourcePath, filterRules });

            _commandChannel.BasicPublish("", RPCQueryName, null, message);

            var subject = new Subject<RecordChangedParam[]>();

            Task.Factory.StartNew(() =>
            {
                var queueName = string.Copy(replyTo);
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var responce = CastTo<RabbitMQResponce>(ea.Body);

                    if (responce.LastMessage) break;

                    var rcpArray = (RecordChangedParam[])responce.Data;

                    subject.OnNext(rcpArray);

                }
                _commandChannel.QueueDelete(queueName);

                subject.OnCompleted();
            });
            return subject.AsObservable();
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            var correlationId = Guid.NewGuid().ToString();

            var onSuccessQueueName = _commandChannel.QueueDeclare().QueueName;

            var onFailQueueName = _commandChannel.QueueDeclare().QueueName;

            var onSuccessConsumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(onSuccessQueueName, false, onSuccessConsumer);

            var onFailConsumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(onFailQueueName, false, onFailConsumer);

            var message = new MethodArgs
             {
                 MethodName = "ResolveRecordbyForeignKey",
                 UserToken = userToken,
                 MethodsArgs = new object[] { changedRecord, dataSourceUrn, onSuccessQueueName, onFailQueueName }
             };

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", RPCQueryName, props, messageBytes);

            var breakFlag = true;

            // Wait for notification on success
            Task.Factory.StartNew(() =>
            {
                while (breakFlag)
                {
                    var ea = onSuccessConsumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var array = BinaryConverter.CastTo<object[]>(ea.Body);

                        if (onSuccess != null)
                        {
                            onSuccess((string)array[0], (RecordChangedParam[])array[1]);
                        }

                        breakFlag = false;
                    }
                }
            });

            // Wait for notification on fail
            Task.Factory.StartNew(() =>
            {
                while (breakFlag)
                {
                    var ea = onFailConsumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
                        var array = BinaryConverter.CastTo<object[]>(ea.Body);

                        if (onSuccess != null)
                        {
                            onSuccess((string)array[0], (RecordChangedParam[])array[1]);
                        }

                        breakFlag = false;
                    }
                }
            });
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

            var message = MethdoArgsToByte(methodName, userToken, methodArgs);

            channel.BasicPublish("", commandQueueName, props, message);

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

        private byte[] MethdoArgsToByte(string methodName, string userToken, object[] methodArgs)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, new MethodArgs
            {
                MethodName = methodName,
                UserToken = userToken,
                MethodsArgs = methodArgs
            });

            return ms.ToArray();
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
