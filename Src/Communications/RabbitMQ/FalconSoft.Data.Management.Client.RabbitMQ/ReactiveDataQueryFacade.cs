using System;
using System.Collections.Generic;
using System.IO;
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
    public class ReactiveDataQueryFacade : IReactiveDataQueryFacade
    {
        private readonly IConnection _connection;
        private readonly IModel _commandChannel;
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        public ReactiveDataQueryFacade(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
        }

        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
             FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetData<T>(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetData(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            var correlationId = Guid.NewGuid().ToString();
            var consumer = new QueueingBasicConsumer(_commandChannel);
            var replyTo = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.BasicConsume(replyTo, true, consumer);

            var props = _commandChannel.CreateBasicProperties();
            props.ReplyTo = replyTo;
            props.CorrelationId = correlationId;

            var message = MethdoArgsToByte("GetData", userToken, new object[] { dataSourcePath, filterRules });

            _commandChannel.BasicPublish("", RPCQueryName, props, message);
            var subject = new Subject<Dictionary<string, object>>();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var responce = CastTo<RabbitMQResponce>(ea.Body);

                    if (responce.LastMessage) break;

                    var list = (List<Dictionary<string, object>>)responce.Data;
                    foreach (var dictionary in list)
                    {
                        subject.OnNext(dictionary);
                    }
                }
                subject.OnCompleted();
            });
            return subject.ToEnumerable();
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            _commandChannel.ExchangeDeclare("GetDataChangesTopic", "topic");

            var queueName = _commandChannel.QueueDeclare().QueueName;
            _commandChannel.QueueBind(queueName, "GetDataChangesTopic", dataSourcePath + "." + userToken);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(queueName, true, consumer);
            
            var message = MethdoArgsToByte("GetDataChanges", userToken, new object[] { dataSourcePath, filterRules });

            _commandChannel.BasicPublish("", RPCQueryName, null, message);
            var subject = new Subject<RecordChangedParam[]>();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();

                    var responce = CastTo<RabbitMQResponce>(ea.Body);

                    if (responce.LastMessage) break;

                    var rcpArray = (RecordChangedParam[])responce.Data;

                    subject.OnNext(rcpArray);

                }
                subject.OnCompleted();
            });
            return subject.AsObservable();
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _connection.Close();
            _commandChannel.Close();
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
