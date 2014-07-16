using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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
        private IConnection _connection;
        private IModel _commandChannel;
        private const string RPCQueryName = "ReactiveDataQueryFacadeRPC";
        private const int Limit = 100;

        public ReactiveDataQueryBroker(string hostName, IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;

            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _commandChannel = _connection.CreateModel();
            _commandChannel.QueueDeclare(RPCQueryName, false, false, false, null);
            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(RPCQueryName, true, consumer);

            while (true)
            {
                var ea = consumer.Queue.Dequeue();
                var message = CastTo<MethodArgs>(ea.Body);

                ExecuteMethodSwitch(message, ea.BasicProperties);
            }
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            switch (message.MethodName)
            {
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
                default: break;
            }
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
                catch (Exception ex)
                {

                }
            });
        }

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
                        var message = new RabbitMQResponce {Data = rcpArgs};
                        _commandChannel.BasicPublish("GetDataChangesTopic",
                            routingKey, null, CastToBytes(message));
                    }, () =>
                    {
                        var userTokenLocal = string.Copy(userToken);
                        var dataSourcePathLocal = string.Copy(dataSourcePath);
                        var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);
                        var message = new RabbitMQResponce {LastMessage = true};
                        _commandChannel.BasicPublish("GetDataChangesTopic",
                            routingKey, null, CastToBytes(message));
                    });
                if (true)
                {
                    var userTokenLocal = string.Copy(userToken);
                    var dataSourcePathLocal = string.Copy(dataSourcePath);
                    var routingKey = string.Format("{0}.{1}", dataSourcePathLocal, userTokenLocal);
                    var message = new RabbitMQResponce
                    {
                        Data = new[]
                        {
                            new RecordChangedParam
                            {
                                RecordKey = "1",
                                ChangeSource = "n",
                                ChangedAction = RecordChangedAction.AddedOrUpdated,
                                OriginalRecordKey = "1",
                                RecordValues = new Dictionary<string, object> {{"id", 1}, {"Value", 32}},
                                UserToken = userTokenLocal
                            },
                        }
                    };
                    _commandChannel.BasicPublish("GetDataChangesTopic",
                        routingKey, null, CastToBytes(message));
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private byte[] CastToBytes(object obj)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, obj);

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
