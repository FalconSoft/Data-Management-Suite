﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class TemporalDataQueryBroker
    {
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        private readonly ILogger _logger;

        private readonly IModel _commandChannel;
        private const string TemporalDataQueryFacadeQueryName = "TemporalDataQueryFacadeRPC";
        private const int Limit = 100;

        public TemporalDataQueryBroker(string hostName, ITemporalDataQueryFacade temporalDataQueryFacade, ILogger logger)
        {
            _temporalDataQueryFacade = temporalDataQueryFacade;
            _logger = logger;

            var factory = new ConnectionFactory { HostName = hostName };

            var connection = factory.CreateConnection();

            _commandChannel = connection.CreateModel();

            _commandChannel.QueueDeclare(TemporalDataQueryFacadeQueryName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);

            _commandChannel.BasicConsume(TemporalDataQueryFacadeQueryName, true, consumer);

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
                case "GetRecordsHistory":
                    {
                        GetRecordsHistory(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0],
                            (string)message.MethodsArgs[1]);
                        break;
                    }
                case "GetDataHistoryByTag":
                    {
                        GetDataHistoryByTag(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0],
                            (TagInfo)message.MethodsArgs[1]);
                        break;
                    }
                case "GetRecordsAsOf":
                    {
                        GetRecordsAsOf(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0],
                            (DateTime)message.MethodsArgs[1]);
                        break;
                    }
                case "GetTemporalDataByRevisionId":
                    {
                        GetTemporalDataByRevisionId(basicProperties, message.UserToken,
                            (DataSourceInfo)message.MethodsArgs[0], message.MethodsArgs[1]);
                        break;
                    }
                case "GetRevisions":
                    {
                        GetRevisions(basicProperties, message.UserToken, (DataSourceInfo)message.MethodsArgs[0]);
                        break;
                    }
                case "GeTagInfos":
                    {
                        GeTagInfos(basicProperties, message.UserToken);
                        break;
                    }
                case "SaveTagInfo":
                    {
                        SaveTagInfo(message.UserToken, (TagInfo)message.MethodsArgs[0]);
                        break;
                    }
                case "RemoveTagInfo":
                    {
                        RemoveTagInfo(message.UserToken, (TagInfo)message.MethodsArgs[0]);
                        break;
                    }
            }
        }

        private void GetRecordsHistory(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, string recordKey)
        {
            Task.Factory.StartNew(() =>
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);

                var replyTo = string.Copy(basicProperties.ReplyTo);

                var userTokenLocal = string.Copy(userToken);

                var recordKeyLocal = string.Copy(recordKey);

                var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                var data = _temporalDataQueryFacade.GetRecordsHistory(dataSourcePathLocal, recordKeyLocal);

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
            });
        }

        private void GetDataHistoryByTag(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            Task.Factory.StartNew(() =>
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);

                var replyTo = string.Copy(basicProperties.ReplyTo);

                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                var data = _temporalDataQueryFacade.GetDataHistoryByTag(dataSourcePathLocal, tagInfo);

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
            });
        }

        private void GetRecordsAsOf(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            Task.Factory.StartNew(() =>
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);

                var replyTo = string.Copy(basicProperties.ReplyTo);

                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                var data = _temporalDataQueryFacade.GetRecordsAsOf(dataSourcePathLocal, timeStamp);

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
            });
        }

        private void GetTemporalDataByRevisionId(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo, object revisionId)
        {
            Task.Factory.StartNew(() =>
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);

                var replyTo = string.Copy(basicProperties.ReplyTo);

                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                var data = _temporalDataQueryFacade.GetTemporalDataByRevisionId(dataSourcePathLocal, revisionId);

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
            });
        }

        private void GetRevisions(IBasicProperties basicProperties, string userToken, DataSourceInfo dataSourceInfo)
        {
            Task.Factory.StartNew(() =>
            {
                var correlationId = string.Copy(basicProperties.CorrelationId);

                var replyTo = string.Copy(basicProperties.ReplyTo);

                var userTokenLocal = string.Copy(userToken);

                var dataSourcePathLocal = (DataSourceInfo)dataSourceInfo.Clone();

                var data = _temporalDataQueryFacade.GetRevisions(dataSourcePathLocal);

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
            });
        }

        private void GeTagInfos(IBasicProperties basicProperties, string userToken)
        {
            var correlationId = string.Copy(basicProperties.CorrelationId);

            var replyTo = string.Copy(basicProperties.ReplyTo);

            var userTokenLocal = string.Copy(userToken);

            var data = _temporalDataQueryFacade.GeTagInfos();

            var array = data.ToArray();

            var props = _commandChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(array));
        }

        private void SaveTagInfo(string userToken, TagInfo tagInfo)
        {
            _temporalDataQueryFacade.SaveTagInfo(tagInfo);
        }

        private void RemoveTagInfo(string userToken, TagInfo tagInfo)
        {
            _temporalDataQueryFacade.RemoveTagInfo(tagInfo);
        }
    }
}
