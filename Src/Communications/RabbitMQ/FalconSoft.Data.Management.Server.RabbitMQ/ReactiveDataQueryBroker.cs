using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class ReactiveDataQueryBroker : IDisposable
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ILogger _logger;
        private IModel _commandChannel;
        private const string ReactiveDataQueryFacadeQueueName = "ReactiveDataQueryFacadeRPC";
        private const int Limit = 100;
        private readonly object _establishConnectionLock = new object();
        private volatile bool _keepAlive = true;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private IConnection _connection;
        private readonly Dictionary<string, DispoceItems> _getDataChangesDispocebles = new Dictionary<string, DispoceItems>();
        private ConnectionFactory connectionFactory;

        public ReactiveDataQueryBroker(string hostName, string username, string pass, IReactiveDataQueryFacade reactiveDataQueryFacade, IMetaDataAdminFacade metaDataAdminFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _metaDataAdminFacade = metaDataAdminFacade;

            _logger = logger;

            connectionFactory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = username,
                Password = pass,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            _connection = connectionFactory.CreateConnection();

            _commandChannel = _connection.CreateModel();

            _commandChannel.QueueDelete(ReactiveDataQueryFacadeQueueName);

            _commandChannel.QueueDeclare(ReactiveDataQueryFacadeQueueName, true, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(ReactiveDataQueryFacadeQueueName, false, consumer);

            Task.Factory.StartNew(() =>
            {
                while (_keepAlive)
                {
                    try
                    {
                        var ea = consumer.Queue.Dequeue();
                        var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);

                        ExecuteMethodSwitch(message, ea.BasicProperties);
                    }
                    catch (EndOfStreamException ex)
                    {
                        _logger.Debug(DateTime.Now + " ReactiveDataQueryBroker failed", ex);

                        lock (_establishConnectionLock)
                        {
                            if (_keepAlive)
                            {
                                _connection = connectionFactory.CreateConnection();
                                _commandChannel = _connection.CreateModel();

                                _commandChannel.QueueDelete(ReactiveDataQueryFacadeQueueName);

                                _commandChannel.QueueDeclare(ReactiveDataQueryFacadeQueueName, true, false, false, null);

                                consumer = new QueueingBasicConsumer(_commandChannel);
                                _commandChannel.BasicConsume(ReactiveDataQueryFacadeQueueName, false, consumer);
                            }
                        }
                    }
                }
            }, _cts.Token);

            Task.Factory.StartNew(() =>
            {
                _metaDataAdminFacade.ObjectInfoChanged += (obj, evArgs) =>
                {
                    if (evArgs.ChangedActionType == ChangedActionType.Delete &&
                        evArgs.ChangedObjectType == ChangedObjectType.DataSourceInfo)
                    {
                        var dataSource = evArgs.SourceObjectInfo as DataSourceInfo;

                        var keys = _getDataChangesDispocebles.Keys.Where(k => k.Contains(dataSource.DataSourcePath));
                        var array = keys as string[] ?? keys.ToArray();
                        if (array.Any())
                        {
                            foreach (string key in array)
                            {
                                _getDataChangesDispocebles[key].Disposable.Dispose();
                                _getDataChangesDispocebles.Remove(key);
                            }
                        }
                    }
                };
            });
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            if (!_keepAlive) return;

            _logger.Debug(string.Format(DateTime.Now + " ReactiveDataQueryBroker. Method Name {0}; User Token {1}; Params {2}",
               message.MethodName,
               message.UserToken ?? string.Empty,
               message.MethodsArgs != null
                   ? message.MethodsArgs.Aggregate("",
                       (cur, next) => cur + " | " + (next != null ? next.ToString() : string.Empty))
                   : string.Empty));

            switch (message.MethodName)
            {
                case "InitializeConnection":
                    {
                        InitializeConnection(basicProperties);
                        break;
                    }
                case "GetAggregatedData":
                    {
                        GetAggregatedData(basicProperties, message.UserToken, message.MethodsArgs[0] as string,
                            message.MethodsArgs[1] as AggregatedWorksheetInfo, message.MethodsArgs[2] as FilterRule[]);
                        break;
                    }
                case "GetData":
                    {
                        GetData(basicProperties, message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string[], message.MethodsArgs[2] as FilterRule[]);
                        break;
                    }
                case "GetDataChanges":
                    {
                        GetDataChanges(message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string[]);
                        break;
                    }
                case "ResolveRecordbyForeignKey":
                    {
                        ResolveRecordbyForeignKey(basicProperties, message.UserToken,
                            message.MethodsArgs[0] as RecordChangedParam[], message.MethodsArgs[1] as string,
                            message.MethodsArgs[2] as string, message.MethodsArgs[3] as string);
                        break;
                    }
                case "CheckExistence":
                    {
                        CheckExistence(basicProperties, message.UserToken, message.MethodsArgs[0] as string,
                            message.MethodsArgs[1] as string, message.MethodsArgs[2]);
                        break;
                    }
                case "GetDataByKey":
                    {
                        GetDataByKey(basicProperties, message.UserToken, message.MethodsArgs[0] as string,
                            message.MethodsArgs[1] as string[]);
                        break;
                    }
                case "Dispose":
                    {
                        Dispose(message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string[]);
                        break;
                    }
            }
        }

        private void Dispose(string userToken, string dataSourcePath, string[] fields = null)
        {
            var routingKey = fields != null
                ? fields.Aggregate(string.Format("{0}.{1}", dataSourcePath, userToken),
                (cur, next) => string.Format("{0}.{1}", cur, next)) : string.Format("{0}.{1}", dataSourcePath, userToken);

            DispoceItems disposer;
            if (_getDataChangesDispocebles.TryGetValue(routingKey, out disposer))
            {
                disposer.Count --;
                if (disposer.Count == 0)
                {
                    disposer.Disposable.Dispose();
                    _getDataChangesDispocebles.Remove(routingKey);
                }
            }
        }

        private void GetDataByKey(IBasicProperties basicProperties, string userToken, string dataSourcePath, string[] recordKeys)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var userTokenLocal = string.Copy(userToken);

                    var dataSourcePathLocal = string.Copy(dataSourcePath);

                    var data = _reactiveDataQueryFacade.GetDataByKey(userTokenLocal, dataSourcePathLocal, recordKeys);

                    var list = new List<Dictionary<string, object>>();

                    var responce = new RabbitMQResponce();

                    foreach (var d in data)
                    {
                        list.Add(d);
                        if (list.Count == Limit)
                        {
                            responce.Id++;
                            responce.Data = list;

                            RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                            list.Clear();
                        }
                    }

                    if (list.Count != 0)
                    {
                        responce.Id++;
                        responce.Data = list;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                        list.Clear();

                        responce.LastMessage = true;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);
                    }
                    else
                    {
                        responce.Id++;
                        responce.LastMessage = true;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetAggregatedData failed", ex);
                    throw;
                }
            }, _cts.Token);
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
            }, _cts.Token);
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
                            responce.Id++;
                            responce.Data = list;

                            RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                            list.Clear();
                        }
                    }

                    if (list.Count != 0)
                    {
                        responce.Id++;
                        responce.Data = list;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                        list.Clear();

                        responce.LastMessage = true;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);
                    }
                    else
                    {
                        responce.Id++;
                        responce.LastMessage = true;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetAggregatedData failed", ex);
                    throw;
                }
            }, _cts.Token);
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetData(IBasicProperties basicProperties, string userToken, string dataSourcePath, string[] fields, FilterRule[] filterRules = null)
        {
            Task.Factory.StartNew(() => GetDataPrivate(basicProperties, userToken, dataSourcePath, fields, filterRules), _cts.Token);
        }

        private void GetDataPrivate(IBasicProperties basicProperties, string userToken, string dataSourcePath, string[] fields, FilterRule[] filterRules)
        {
            try
            {
                var replyTo = basicProperties.ReplyTo;

                var correlationId = basicProperties.CorrelationId;

                var userTokenLocal = userToken;

                var dataSourcePathLocal = dataSourcePath;

                var data = _reactiveDataQueryFacade.GetData(userTokenLocal, dataSourcePathLocal, fields, filterRules: filterRules);

                var list = new List<Dictionary<string, object>>();

                var responce = new RabbitMQResponce();

                foreach (var d in data)
                {
                    list.Add(d);
                    if (list.Count == Limit)
                    {
                        responce.Id++;
                        responce.Data = list;

                        RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                        list.Clear();
                    }
                }
                if (list.Count != 0)
                {
                    responce.Id++;
                    responce.Data = list;

                    RPCSendTaskExecutionResults(replyTo, correlationId, responce);

                    list.Clear();


                    RPCSendTaskExecutionResults(replyTo, correlationId, new RabbitMQResponce { Id = responce.Id++, LastMessage = true });
                }
                else
                {
                    responce.Id++;
                    responce.LastMessage = true;

                    RPCSendTaskExecutionResults(replyTo, correlationId, new RabbitMQResponce { Id = responce.Id++, LastMessage = true });
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("GetData failed", ex);
                throw;
            }
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            try
            {
                var routingKey = fields != null ? fields.Aggregate(string.Format("{0}.{1}", dataSourcePath, userToken),
               (cur, next) => string.Format("{0}.{1}", cur, next)) : string.Format("{0}.{1}", dataSourcePath, userToken);

                if (_getDataChangesDispocebles.ContainsKey(routingKey))
                {
                    _getDataChangesDispocebles[routingKey].Count++;
                    return;
                };

                _commandChannel.ExchangeDeclare("GetDataChangesTopic", "topic");


                var disposer = _reactiveDataQueryFacade.GetDataChanges(userToken, dataSourcePath, fields).Subscribe(
                    rcpArgs =>
                    {
                        lock (_establishConnectionLock)
                        {
                            var message = new RabbitMQResponce { Data = rcpArgs };

                            try
                            {
                                _commandChannel.BasicPublish("GetDataChangesTopic",
                                    routingKey, null, BinaryConverter.CastToBytes(message));
                            }
                            catch (Exception)
                            {
                                _connection = connectionFactory.CreateConnection();
                                _commandChannel = _connection.CreateModel();
                            }
                        }
                    }, () =>
                    {
                        lock (_establishConnectionLock)
                        {
                            var message = new RabbitMQResponce { LastMessage = true };

                            _commandChannel.BasicPublish("GetDataChangesTopic",
                                routingKey, null, BinaryConverter.CastToBytes(message));
                        }
                    });

                _getDataChangesDispocebles.Add(routingKey, new DispoceItems{Count = 1, Disposable = disposer});
            }
            catch (Exception ex)
            {
                _logger.Debug("GetData failed", ex);
            }
        }

        private void ResolveRecordbyForeignKey(IBasicProperties basicProperties,
            string userToken,
            RecordChangedParam[] changedRecords,
            string dataSourcePath,
            string onSuccessQueueName,
            string onFailQueueName)
        {
            try
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);
                var onSuccessQueueNameLocal = string.Copy(onSuccessQueueName);
                var onFailQueueNameLocal = string.Copy(onFailQueueName);

                var onSuccess = new Action<string, RecordChangedParam[]>((str, rcpArray) =>
                {
                    var message = new object[] { str, rcpArray };

                    RPCSendTaskExecutionResults(onSuccessQueueNameLocal, correlationId, message);
                });

                var onFail = new Action<string, Exception>((str, exception) =>
                {
                    var message = new object[] { str, exception };

                    RPCSendTaskExecutionResults(onFailQueueNameLocal, correlationId, message);
                });

                _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecords, dataSourcePath, userToken, onSuccess,
                    onFail);
            }
            catch (Exception ex)
            {
                _logger.Debug("ResolveRecordbyForeignKey failed", ex);
            }
        }

        private void CheckExistence(IBasicProperties basicProperties, string userToken, string dataSourceUrn, string fieldName, object value)
        {
            Task.Factory.StartNew(obj =>
            {
                var replyTo = string.Copy(basicProperties.ReplyTo);

                var correlationId = string.Copy(basicProperties.CorrelationId);

                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = string.Copy(dataSourceUrn);

                var fieldNameLockal = string.Copy(fieldName);

                var checkResult = _reactiveDataQueryFacade.CheckExistence(userTokenLocal, dataSourcePathLocal, fieldNameLockal, obj);

                RPCSendTaskExecutionResults(replyTo, correlationId, checkResult);

            }, value);
        }

        private void RPCSendTaskExecutionResults<T>(string replyTo, string correlationId, T data)
        {
            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, data);

            var messageBytes = ms.ToArray();

            _commandChannel.BasicPublish("", replyTo, props, messageBytes);
        }

        public void Dispose()
        {
            _keepAlive = false;

            foreach (var dispoceble in _getDataChangesDispocebles.Values)
            {
                dispoceble.Disposable.Dispose();
            }

            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
        }

        class DispoceItems
        {
            public int Count { get; set; }

            public IDisposable Disposable { get; set; }
        }
    }
}
