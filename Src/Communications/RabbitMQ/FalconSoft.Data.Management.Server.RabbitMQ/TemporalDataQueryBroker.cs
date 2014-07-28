using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class TemporalDataQueryBroker : IDisposable
    {
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        private readonly ILogger _logger;

        private readonly IModel _commandChannel;
        private Task _task;
        private const string TemporalDataQueryFacadeQueryName = "TemporalDataQueryFacadeRPC";
        private const int Limit = 100;

        public TemporalDataQueryBroker(string hostName, string userName, string password, ITemporalDataQueryFacade temporalDataQueryFacade, ILogger logger)
        {
            _temporalDataQueryFacade = temporalDataQueryFacade;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                VirtualHost = "/",
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };

            var connection = factory.CreateConnection();

            _commandChannel = connection.CreateModel();

            _commandChannel.QueueDeclare(TemporalDataQueryFacadeQueryName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
           
            _commandChannel.BasicConsume(TemporalDataQueryFacadeQueryName, true, consumer);

            _task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    var message = BinaryConverter.CastTo<MethodArgs>(ea.Body);

                    ExecuteMethodSwitch(message, ea.BasicProperties);
                }
            });
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
                case "GetRecordsHistory":
                    {
                        GetRecordsHistory(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo,
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "GetDataHistoryByTag":
                    {
                        GetDataHistoryByTag(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo,
                            (TagInfo)message.MethodsArgs[1]);
                        break;
                    }
                case "GetRecordsAsOf":
                    {
                        GetRecordsAsOf(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo,
                            (DateTime)message.MethodsArgs[1]);
                        break;
                    }
                case "GetTemporalDataByRevisionId":
                    {
                        GetTemporalDataByRevisionId(basicProperties, message.UserToken,
                            message.MethodsArgs[0] as DataSourceInfo, message.MethodsArgs[1]);
                        break;
                    }
                case "GetRevisions":
                    {
                        GetRevisions(basicProperties, message.UserToken, message.MethodsArgs[0] as DataSourceInfo);
                        break;
                    }
                case "GeTagInfos":
                    {
                        GeTagInfos(basicProperties, message.UserToken);
                        break;
                    }
                case "SaveTagInfo":
                    {
                        SaveTagInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as TagInfo);
                        break;
                    }
                case "RemoveTagInfo":
                    {
                        RemoveTagInfo(basicProperties, message.UserToken, message.MethodsArgs[0] as TagInfo);
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
            });
        }

        private void GetRecordsHistory(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, string recordKey)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var recordKeyLocal = string.Copy(recordKey);

                    var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                    var data = _temporalDataQueryFacade.GetRecordsHistory(dataSourcePathLocal, recordKeyLocal);

                    RPCEnumerableSendOfTaskResult(replyTo, correlationId, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetRecordsHistory failed", ex);
                    throw;
                }
            });
        }

        private void GetDataHistoryByTag(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                    var data = _temporalDataQueryFacade.GetDataHistoryByTag(dataSourcePathLocal, tagInfo);

                    RPCEnumerableSendOfTaskResult(replyTo, correlationId, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetDataHistoryByTag failed", ex);
                    throw;
                }
            });
        }

        private void GetRecordsAsOf(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                    var data = _temporalDataQueryFacade.GetRecordsAsOf(dataSourcePathLocal, timeStamp);

                    RPCEnumerableSendOfTaskResult(replyTo, correlationId, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetRecordsAsOf failed", ex);
                    throw;
                }
            });
        }

        private void GetTemporalDataByRevisionId(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, object revisionId)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                    var data = _temporalDataQueryFacade.GetTemporalDataByRevisionId(dataSourcePathLocal, revisionId);

                    RPCEnumerableSendOfTaskResult(replyTo, correlationId, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetTemporalDataByRevisionId failed", ex);
                    throw;
                }
            });
        }

        private void GetRevisions(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                    var data = _temporalDataQueryFacade.GetRevisions(dataSourcePathLocal);

                    RPCEnumerableSendOfTaskResult(replyTo, correlationId, data);
                }
                catch (Exception ex)
                {
                    _logger.Debug("GetRevisions failed", ex);
                    throw;
                }
            });
        }

        private void GeTagInfos(IBasicProperties basicProperties, string userToken)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;

                    var data = _temporalDataQueryFacade.GeTagInfos();

                    var array = data.ToArray();

                    _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(array));
                }
                catch (Exception ex)
                {
                    _logger.Debug("GeTagInfos failed", ex);
                    throw;
                }
            });
        }

        private void SaveTagInfo(IBasicProperties basicProperties, string userToken, TagInfo tagInfo)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    _temporalDataQueryFacade.SaveTagInfo(tagInfo);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("SaveTagInfo failed", ex);
                    throw;
                }
            });
        }

        private void RemoveTagInfo(IBasicProperties basicProperties, string userToken, TagInfo tagInfo)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var correlationId = string.Copy(basicProperties.CorrelationId);

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = correlationId;
                    props.ReplyTo = replyTo;

                    _temporalDataQueryFacade.RemoveTagInfo(tagInfo);

                    RPCSendTaskExecutionFinishNotification(props);
                }
                catch (Exception ex)
                {
                    _logger.Debug("SaveTagInfo failed", ex);
                    throw;
                }
            });
        }

        private void RPCSendTaskExecutionFinishNotification(IBasicProperties basicProperties)
        {
            var correlationId = basicProperties.CorrelationId;

            var replyTo = basicProperties.ReplyTo;

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, null);
        }

        private void RPCEnumerableSendOfTaskResult(string replyTo, string correlationId, IEnumerable<Dictionary<string, object>> data)
        {
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

                    _commandChannel.BasicPublish("", replyTo, props, CastToBytes(responce));

                    list.Clear();
                }
            }

            if (list.Count != 0)
            {
                var props = _commandChannel.CreateBasicProperties();
                props.CorrelationId = correlationId;

                responce.Id++;
                responce.Data = list;

                _commandChannel.BasicPublish("", replyTo, props, CastToBytes(responce));

                list.Clear();

                responce.LastMessage = true;

                _commandChannel.BasicPublish("", replyTo, props, CastToBytes(responce));
            }
            else
            {
                var props = _commandChannel.CreateBasicProperties();
                props.CorrelationId = correlationId;

                responce.Id++;
                responce.LastMessage = true;

                _commandChannel.BasicPublish("", replyTo, props, CastToBytes(responce));
            }
        }

        private byte[] CastToBytes(RabbitMQResponce responce)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, responce);

            return ms.ToArray();
        }

        public void Dispose()
        {
            _task.Dispose();
        }
    }
}
