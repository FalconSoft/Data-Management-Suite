using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ILogger _logger;
        private IModel _commandChannel;
        private const string ReactiveDataQueryFacadeQueueName = "ReactiveDataQueryFacadeRPC";
        private const int Limit = 100;
        private readonly object _establishConnectionLock = new object();
        private bool _keepAlive = true;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private IConnection _connection;

        public ReactiveDataQueryBroker(string hostName, string username, string pass, IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = username,
                Password = pass,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort,
                RequestedHeartbeat = 30
            };

            _connection = factory.CreateConnection();

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
                        _logger.Debug("ReactiveDataQueryBroker failed", ex);

                        lock (_establishConnectionLock)
                        {
                            if (_keepAlive)
                            {
                                _connection = factory.CreateConnection();
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
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
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
                        GetData(basicProperties, message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as FilterRule[]);
                        break;
                    }
                case "GetDataChanges":
                    {
                        GetDataChanges(message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as FilterRule[]);
                        break;
                    }
                case "ResolveRecordbyForeignKey":
                    {
                        ResolveRecordbyForeignKey(basicProperties, message.UserToken,
                            message.MethodsArgs[0] as RecordChangedParam[], message.MethodsArgs[1] as string,
                            message.MethodsArgs[2] as string, message.MethodsArgs[3] as string);
                        break;
                    }
            }
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
            }, _cts.Token);
        }

        // TODO: filter rules do not catche by thread and coud be changed while thread is running
        private void GetDataChanges(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            try
            {
                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = string.Copy(dataSourcePath);

                var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);

                _commandChannel.ExchangeDeclare("GetDataChangesTopic", "topic");

                _reactiveDataQueryFacade.GetDataChanges(userToken, dataSourcePath, filterRules).Subscribe(
                    rcpArgs =>
                    {
                        lock (_establishConnectionLock)
                        {
                            var message = new RabbitMQResponce {Data = rcpArgs};

                            _commandChannel.BasicPublish("GetDataChangesTopic",
                                routingKey, null, BinaryConverter.CastToBytes(message));
                        }
                    }, () =>
                    {
                        lock (_establishConnectionLock)
                        {
                            var message = new RabbitMQResponce {LastMessage = true};

                            _commandChannel.BasicPublish("GetDataChangesTopic",
                                routingKey, null, BinaryConverter.CastToBytes(message));
                        }
                    });
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
            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Abort();
            _connection.Close();
        }
    }
}
