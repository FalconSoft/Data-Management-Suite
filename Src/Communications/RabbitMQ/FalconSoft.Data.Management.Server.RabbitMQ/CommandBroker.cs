using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public sealed class CommandBroker : IDisposable
    {
        private readonly ICommandFacade _commandFacade;
        private readonly ILogger _logger;
        private IModel _commandChannel;
        private const string CommandFacadeQueueName = "CommandFacadeRPC";
        private volatile bool _keepAlive = true;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private IConnection _connection;

        public CommandBroker(string hostName, string userName, string password, ICommandFacade commandFacade, ILogger logger)
        {
            _commandFacade = commandFacade;
            _logger = logger;

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

            _commandChannel.QueueDelete(CommandFacadeQueueName);

            _commandChannel.QueueDeclare(CommandFacadeQueueName, true, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(CommandFacadeQueueName, false, consumer);

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
                        _logger.Debug("CommandBroker", ex);

                        if (_keepAlive)
                        {
                            _connection = factory.CreateConnection();

                            _commandChannel = _connection.CreateModel();

                            _commandChannel.QueueDelete(CommandFacadeQueueName);

                            _commandChannel.QueueDeclare(CommandFacadeQueueName, true, false, false, null);

                            consumer = new QueueingBasicConsumer(_commandChannel);
                            _commandChannel.BasicConsume(CommandFacadeQueueName, false, consumer);
                        }
                    }
                }
            }, _cts.Token);
        }

        private void ExecuteMethodSwitch(MethodArgs message, IBasicProperties basicProperties)
        {
            if (!_keepAlive) return;

            switch (message.MethodName)
            {
                case "InitializeConnection":
                    {
                        InitializeConnection(basicProperties);
                        break;
                    }
                case "SubmitChanges":
                    {
                        SubmitChanges(basicProperties, message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string, message.MethodsArgs[2] as string);
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
                    _logger.Debug(DateTime.Now + " Command Broker. InitializeConnection starts");

                    var replyTo = string.Copy(basicProperties.ReplyTo);

                    var corelationId = string.Copy(basicProperties.CorrelationId);

                    var props = _commandChannel.CreateBasicProperties();
                    props.CorrelationId = corelationId;

                    _commandChannel.BasicPublish("", replyTo, props, null);
                }
                catch (Exception ex)
                {
                    _logger.Debug(DateTime.Now + " Failed to responce to client connection confirming.", ex);
                    throw;
                }
            }, _cts.Token);
        }

        private void SubmitChanges(IBasicProperties basicProperties, string userToken, string dataSourcePath,
            string toUpdateQueueName, string toDeleteQueuName)
        {
            try
            {
                _logger.Debug(DateTime.Now + " Command Broker. SubmitChanges starts");

                var toUpdateDataSubject = new Subject<Dictionary<string, object>>();
                var toDeleteDataSubject = new Subject<string>();

                var toUpdateQueueNameLocal = toUpdateQueueName;
                var toDeleteQueueNameLocal = toDeleteQueuName;

                var changeRecordsEnumerator = toUpdateDataSubject.ToEnumerable();
                var deletedEnumerator = toDeleteDataSubject.ToEnumerable();

                var are1 = toUpdateQueueNameLocal != null ? new AutoResetEvent(false) : null;
                var are2 = toDeleteQueueNameLocal != null ? new AutoResetEvent(false) : null;

                var task1 = Task.Factory.StartNew(
                    () =>
                    {
                        if (are1 != null) are1.Set();
                        Trace.WriteLine("********   To Array call! " + DateTime.Now.ToString("fffff"));
                        return toUpdateQueueNameLocal != null ? changeRecordsEnumerator.ToArray() : null;
                    });

                var task2 = Task.Factory.StartNew(() =>
                {
                    if (are2 != null) are2.Set();
                    return toDeleteQueueNameLocal != null ? deletedEnumerator.ToArray() : null;
                });

                //initialise submit changes method work.
                Task.Factory.StartNew(() =>
                {
                    var replyTo = basicProperties.ReplyTo;
                    var corelationId = basicProperties.CorrelationId;
                    var userTokenLocal = userToken;
                    var dataSourcePathLocal = dataSourcePath;

                    var changedRecords = task1.Result;
                    var deleted = task2.Result;

                    _logger.Debug(string.Format("{2} Command Broker. SubmitChanges, data to change count : {0}; data to delete count : {1}", changedRecords != null ? changedRecords.Count() : 0, deleted != null ? deleted.Count() : 0, DateTime.Now));

                    _commandFacade.SubmitChanges(dataSourcePathLocal, userTokenLocal, changedRecords, deleted, ri =>
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = corelationId;
                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(ri));
                    }, ex =>
                    {
                        
                    });
                }, _cts.Token);


                //collect changed records
                if (toUpdateQueueNameLocal != null)
                {
                    var con = new QueueingBasicConsumer(_commandChannel);
                    _commandChannel.BasicConsume(toUpdateQueueNameLocal, false, con);

                    Task.Factory.StartNew(obj =>
                    {
                        var consumer = (QueueingBasicConsumer)obj;

                        are1.WaitOne();
                        are1.Dispose();

                        while (true)
                        {
                            var ea = consumer.Queue.Dequeue();

                            using (var memStream = new MemoryStream())
                            {
                                var binForm = new BinaryFormatter();
                                memStream.Write(ea.Body, 0, ea.Body.Length);
                                memStream.Seek(0, SeekOrigin.Begin);

                                var responce = (RabbitMQResponce) binForm.Deserialize(memStream);

                                if (responce.LastMessage)
                                {
                                    break;
                                }

                                var data = (List<Dictionary<string, object>>) responce.Data;

                                foreach (var dictionary in data)
                                {
                                    toUpdateDataSubject.OnNext(dictionary);
                                }

                            }
                        }
                    }, con, _cts.Token).ContinueWith(t =>
                    {
                        toUpdateDataSubject.OnCompleted();
                        Trace.WriteLine("****** Complete call! " + DateTime.Now.ToString("fffff"));
                    });
                }

                //collect record keys to delete
                if (toDeleteQueueNameLocal != null)
                {
                    var con = new QueueingBasicConsumer(_commandChannel);
                    _commandChannel.BasicConsume(toDeleteQueueNameLocal, false, con);

                    Task.Factory.StartNew(obj =>
                    {
                        var consumer = (QueueingBasicConsumer)obj;

                        are2.WaitOne();
                        are2.Dispose();

                        while (true)
                        {
                            var ea = consumer.Queue.Dequeue();

                            using (var memStream = new MemoryStream())
                            {
                                var binForm = new BinaryFormatter();
                                memStream.Write(ea.Body, 0, ea.Body.Length);
                                memStream.Seek(0, SeekOrigin.Begin);

                                var responce = (RabbitMQResponce) binForm.Deserialize(memStream);

                                if (responce.LastMessage)
                                {
                                    break;
                                }
                                var data = (List<string>) responce.Data;

                                foreach (var dictionary in data)
                                {
                                    toDeleteDataSubject.OnNext(dictionary);
                                }
                            }
                        }
                    }, con, _cts.Token).ContinueWith(t => toDeleteDataSubject.OnCompleted()); ;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(DateTime.Now + " SubmitChanges failed", ex);
            }
        }

        public void Dispose()
        {
            _logger.Debug(DateTime.Now + " Command Broker Disposed.");

            _keepAlive = false;
            _cts.Cancel();
            _cts.Dispose();
            _commandChannel.Close();
            _connection.Close();
        }
    }
}
