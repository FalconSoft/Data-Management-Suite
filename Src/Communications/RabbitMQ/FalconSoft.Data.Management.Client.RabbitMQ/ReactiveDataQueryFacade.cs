using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class ReactiveDataQueryFacade : IReactiveDataQueryFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public ReactiveDataQueryFacade(string hostName, string userName, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();

            InitializeConnection(RPCQueryName);
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

        public IEnumerable<Dictionary<string, object>> GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys)
        {
            return RPCServerTaskExecute<Dictionary<string, object>>(_connection, RPCQueryName, "GetDataByKey", userToken,
               new object[] { dataSourcePath, recordKeys });
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

            var observable = Observable.Create<RecordChangedParam[]>(subj =>
            {
                var keepAlive = true;
                Task.Factory.StartNew(() =>
                {
                    while (keepAlive)
                    {
                        try
                        {
                            var ea = consumer.Queue.Dequeue();

                            var responce = CastTo<RabbitMQResponce>(ea.Body);

                            if (responce.LastMessage) break;

                            var rcpArray = (RecordChangedParam[]) responce.Data;

                            subj.OnNext(rcpArray);
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                    }
                   
                }, _cts.Token);
                return Disposable.Create(() =>
                {
                    keepAlive = false;
                    consumer.OnCancel();
                    subj.OnCompleted();
                });
            });


            return observable;
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
            props.SetPersistent(true);

            var messageBytes = BinaryConverter.CastToBytes(message);

            _commandChannel.BasicPublish("", RPCQueryName, props, messageBytes);

            var breakFlag = true;

            // Wait for notification on success
            Task.Factory.StartNew(() =>
            {
                while (breakFlag)
                {
                    try
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
                    catch (EndOfStreamException ex)
                    {
                        return;
                    }
                }
            }, _cts.Token);

            // Wait for notification on fail
            Task.Factory.StartNew(() =>
            {
                while (breakFlag)
                {
                    try
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
                    catch (EndOfStreamException ex)
                    {
                        return;
                    }
                }
            }, _cts.Token);
        }

        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            using (var channel = _connection.CreateModel())
            {
                var correlationId = Guid.NewGuid().ToString();

                var replyTo = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyTo;

                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(replyTo, true, consumer);

                var message = new MethodArgs
                {
                    MethodName = "CheckExistence",
                    UserToken = userToken,
                    MethodsArgs = new[] { dataSourceUrn, fieldName, value }
                };

                var messageBytes = BinaryConverter.CastToBytes(message);

                channel.BasicPublish("", RPCQueryName, props, messageBytes);

                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        var responce = BinaryConverter.CastTo<bool>(ea.Body);

                        return responce;
                    }
                }
            }
        }

        public void Dispose()
        {

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
            props.SetPersistent(true);

            var message = MethdoArgsToByte(methodName, userToken, methodArgs);

            channel.BasicPublish("", commandQueueName, props, message);

            var subject = new Subject<T>();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    if (correlationId == ea.BasicProperties.CorrelationId)
                    {
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
                }
                subject.OnCompleted();
            }, _cts.Token);

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
            if (!byteArray.Any())
                return default(T);

            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return (T)binForm.Deserialize(memStream);
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

                channel.BasicPublish("", replyTo, props, messageBytes);

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
            _cts.Cancel();
            _commandChannel.Close();
            _connection.Close();
        }
    }
}
