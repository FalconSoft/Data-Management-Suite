using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class ReactiveDataQueryBroker
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ILogger _logger;
        private readonly IModel _commandChannel;
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        private const int Limit = 100;

        public ReactiveDataQueryBroker(string hostName, IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;

            var factory = new ConnectionFactory { HostName = hostName };
            IConnection connection = factory.CreateConnection();
            _commandChannel = connection.CreateModel();
            _commandChannel.QueueDeclare(RPCQueryName, false, false, false, null);
            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(RPCQueryName, true, consumer);

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
                case "GetAggregatedData":
                    {
                        GetAggregatedData(basicProperties, message.UserToken, (string)message.MethodsArgs[0],
                            (AggregatedWorksheetInfo)message.MethodsArgs[1], (FilterRule[])message.MethodsArgs[2]);
                        break;
                    }
                case "GetData":
                    {
                        GetData(basicProperties, message.UserToken, (string)message.MethodsArgs[0], message.MethodsArgs[1] as FilterRule[]);
                        break;
                    }
                case "GetDataChanges":
                    {
                        GetDataChanges(message.UserToken, (string)message.MethodsArgs[0], message.MethodsArgs[1] as FilterRule[]);
                        break;
                    }
                case "ResolveRecordbyForeignKey":
                    {
                        ResolveRecordbyForeignKey(basicProperties, message.UserToken,
                            (RecordChangedParam[])message.MethodsArgs[0], (string)message.MethodsArgs[1],
                            (string)message.MethodsArgs[2], (string)message.MethodsArgs[3]);
                        break;
                    }
            }
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetAggregatedData(IBasicProperties basicProperties, string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheetInfo, FilterRule[] filterRules)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var userTokenLocal = string.Copy(userToken);

                    var dataSourcePathLocal = string.Copy(dataSourcePath);

                    var data = _reactiveDataQueryFacade.GetAggregatedData(userTokenLocal, dataSourcePathLocal, aggregatedWorksheetInfo, filterRules);

                    var list = new List<Dictionary<string, object>>();

                    var responce = new RabbitMQResponce();

                    foreach (var d in data)
                    {
                        list.Add(d);
                        if (list.Count == Limit)
                        {
                            var props = _commandChannel.CreateBasicProperties();
                            props.CorrelationId = correlationId;

                            responce.Id++;
                            responce.Data = list;

                            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));

                            list.Clear();
                        }
                    }

                    if (list.Count != 0)
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = correlationId;

                        responce.Id++;
                        responce.Data = list;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));

                        list.Clear();

                        responce.LastMessage = true;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));
                    }
                    else
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = correlationId;

                        responce.Id++;
                        responce.LastMessage = true;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));
                    }
                }
                catch (Exception ex)
                {

                }
            });
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetData(IBasicProperties basicProperties, string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var userTokenLocal = string.Copy(userToken);

                    var dataSourcePathLocal = string.Copy(dataSourcePath);

                    var data = _reactiveDataQueryFacade.GetData(userTokenLocal, dataSourcePathLocal, filterRules);

                    var list = new List<Dictionary<string, object>>();

                    var responce = new RabbitMQResponce();

                    foreach (var d in data)
                    {
                        list.Add(d);
                        if (list.Count == Limit)
                        {
                            var props = _commandChannel.CreateBasicProperties();
                            props.CorrelationId = correlationId;

                            responce.Id++;
                            responce.Data = list;

                            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));

                            list.Clear();
                        }
                    }
                    if (list.Count != 0)
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = correlationId;

                        responce.Id++;
                        responce.Data = list;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));

                        list.Clear();

                        responce.LastMessage = true;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));
                    }
                    else
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = correlationId;

                        responce.Id++;
                        responce.LastMessage = true;

                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(responce));
                    }
                }
                catch (Exception ex)
                {

                }
            });
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetDataChanges(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            try
            {
                _commandChannel.ExchangeDeclare("GetDataChangesTopic", "topic");

                _reactiveDataQueryFacade.GetDataChanges(userToken, dataSourcePath, filterRules).Subscribe(
                    rcpArgs =>
                    {
                        var userTokenLocal = string.Copy(userToken);

                        var dataSourcePathLocal = string.Copy(dataSourcePath);

                        var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);

                        var message = new RabbitMQResponce { Data = rcpArgs };

                        _commandChannel.BasicPublish("GetDataChangesTopic",
                            routingKey, null, BinaryConverter.CastToBytes(message));
                    }, () =>
                    {
                        var userTokenLocal = string.Copy(userToken);

                        var dataSourcePathLocal = string.Copy(dataSourcePath);

                        var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);

                        var message = new RabbitMQResponce { LastMessage = true };

                        _commandChannel.BasicPublish("GetDataChangesTopic",
                            routingKey, null, BinaryConverter.CastToBytes(message));
                    });
                //This just return bak one value to test connection.
                //if (true)
                //{
                //    var userTokenLocal = string.Copy(userToken);
                //    var dataSourcePathLocal = string.Copy(dataSourcePath);
                //    var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);
                //    var message = new RabbitMQResponce
                //    {
                //        Data = new[]
                //        {
                //            new RecordChangedParam
                //            {
                //                RecordKey = "1",
                //                ChangeSource = "n",
                //                ChangedAction = RecordChangedAction.AddedOrUpdated,
                //                OriginalRecordKey = "1",
                //                RecordValues = new Dictionary<string, object> {{"id", 1}, {"Value", 32}},
                //                UserToken = userTokenLocal
                //            },
                //        }
                //    };
                //    _commandChannel.BasicPublish("GetDataChangesTopic",
                //        routingKey, null, CastToBytes(message));
                //}
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ResolveRecordbyForeignKey(IBasicProperties basicProperties,
            string userToken,
            RecordChangedParam[] changedRecords,
            string dataSourcePath,
            string onSuccessQueueName,
            string onFailQueueName)
        {
            var correlationId = string.Copy(basicProperties.CorrelationId);
            var onSuccessQueueNameLocal = string.Copy(onSuccessQueueName);
            var onFailQueueNameLocal = string.Copy(onFailQueueName);

            var onSuccess = new Action<string, RecordChangedParam[]>((str, rcpArray) =>
            {
                var props = _commandChannel.CreateBasicProperties();
                props.CorrelationId = correlationId;

                var message = new object[] { str, rcpArray };

                var messageBytes = BinaryConverter.CastToBytes(message);

                _commandChannel.BasicPublish("", onSuccessQueueNameLocal, props, messageBytes);
            });

            var onFail = new Action<string, Exception>((str, exception) =>
            {
                var props = _commandChannel.CreateBasicProperties();
                props.CorrelationId = correlationId;

                var message = new object[] { str, exception };

                var messageBytes = BinaryConverter.CastToBytes(message);

                _commandChannel.BasicPublish("", onFailQueueNameLocal, props, messageBytes);
            });

            _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecords, dataSourcePath, userToken, onSuccess,
                onFail);
        }
    }
}
