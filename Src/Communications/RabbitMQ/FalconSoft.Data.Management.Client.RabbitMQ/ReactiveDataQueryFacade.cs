using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class ReactiveDataQueryFacade : RabbitMQFacadeBase, IReactiveDataQueryFacade
    {
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        private const string GetDataChangesTopic = "GetDataChangesTopic";

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private const int TimeOut = 5000;

        public ReactiveDataQueryFacade(string hostName, string userName, string password)
            : base(hostName, userName, password)
        {
            InitializeConnection(RPCQueryName);
            KeepAliveAction = () => InitializeConnection(RPCQueryName);
        }

        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
             FilterRule[] filterRules = null)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, RPCQueryName, "GetAggregatedData", userToken,
                new object[] { dataSourcePath, aggregatedWorksheet, filterRules });
        }

        public IEnumerable<T> GetData<T>(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetData(string userToken, string dataSourcePath, string[] fields = null, FilterRule[] filterRules = null)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, RPCQueryName, "GetData", userToken,
                new object[] { dataSourcePath, fields, filterRules });
        }

        public IEnumerable<string> GetFieldData(string userToken, string dataSourcePath, string field, string match, int elementsToReturn = 10)
        {
            return RPCServerTaskExecuteEnumerable<string>(Connection, RPCQueryName, "GetFieldData", userToken,
                new object[] { dataSourcePath, field, match, elementsToReturn });
        }

        public IEnumerable<Dictionary<string, object>> GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, RPCQueryName, "GetDataByKey", userToken,
               new object[] { dataSourcePath, recordKeys });
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            var routingKey = fields != null ? fields.Aggregate(string.Format("{0}.{1}", dataSourcePath, userToken),
                (cur, next) => string.Format("{0}.{1}", cur, next)).GetHashCode().ToString() : string.Format("{0}.{1}", dataSourcePath, userToken);

            var observable = CreateExchngeObservable<RecordChangedParam[]>(CommandChannel, GetDataChangesTopic,
                "topic", routingKey, RPCQueryName, "GetDataChanges", userToken, new object[] { dataSourcePath, fields });

            return observable;
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            var correlationId = Guid.NewGuid().ToString();

            var onSuccessQueueName = CommandChannel.QueueDeclare().QueueName;

            var onFailQueueName = CommandChannel.QueueDeclare().QueueName;

            var onSuccessConsumer = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(onSuccessQueueName, false, onSuccessConsumer);

            var onFailConsumer = new QueueingBasicConsumer(CommandChannel);
            CommandChannel.BasicConsume(onFailQueueName, false, onFailConsumer);

            var message = new MethodArgs
             {
                 MethodName = "ResolveRecordbyForeignKey",
                 UserToken = userToken,
                 MethodsArgs = new object[] { changedRecord, dataSourceUrn, onSuccessQueueName, onFailQueueName }
             };

            var props = CommandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.SetPersistent(true);

            var messageBytes = BinaryConverter.CastToBytes(message);

            CommandChannel.BasicPublish("", RPCQueryName, props, messageBytes);

            var breakFlag = true;

            // Wait for notification on success
            Task.Factory.StartNew(() =>
            {
                while (breakFlag)
                {
                    try
                    {
                        BasicDeliverEventArgs ea;
                        if (onSuccessConsumer.Queue.Dequeue(TimeOut, out ea))
                        {
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
                        else
                        {
                            if (onFail != null)
                            {
                                onFail("Connection to server is broken", new TimeoutException("TimeOut for respoce elapsed!"));
                            }

                            breakFlag = false;
                        }
                    }
                    catch (EndOfStreamException)
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
                        BasicDeliverEventArgs ea;
                        if (onFailConsumer.Queue.Dequeue(TimeOut, out ea))
                        {
                            if (correlationId == ea.BasicProperties.CorrelationId)
                            {
                                var array = BinaryConverter.CastTo<object[]>(ea.Body);

                                if (onFail != null)
                                {
                                    onFail((string)array[0], (Exception)array[1]);
                                }

                                breakFlag = false;
                            }
                        }
                        else
                        {
                            if (breakFlag && onFail != null)
                            {
                                onFail("Connection to server is broken", new TimeoutException("TimeOut for respoce elapsed!"));
                            }

                            breakFlag = false;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        return;
                    }
                }
            }, _cts.Token);
        }

        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            return RPCServerTaskExecute<bool>(Connection, RPCQueryName, "CheckExistence", userToken,
                new[] { dataSourceUrn, fieldName, value });
        }

        public void Dispose()
        {

        }

        public new void Close()
        {
            _cts.Cancel();
            _cts.Dispose();

            base.Close();
        }
    }
}
