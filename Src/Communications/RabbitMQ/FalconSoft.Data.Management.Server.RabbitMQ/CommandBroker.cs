using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    public class CommandBroker : IDisposable
    {
        private readonly ICommandFacade _commandFacade;
        private readonly ILogger _logger;
        private readonly IModel _commandChannel;
        private Task _task;
        private const string CommandFacadeQueueName = "CommandFacadeRPC";
        public CommandBroker(string hostName, string userName, string password, ICommandFacade commandFacade, ILogger logger)
        {
            _commandFacade = commandFacade;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Protocol = Protocols.FromEnvironment(),
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            var connection = factory.CreateConnection();
            _commandChannel = connection.CreateModel();
            
            _commandChannel.QueueDelete(CommandFacadeQueueName);

            _commandChannel.QueueDeclare(CommandFacadeQueueName, false, false, false, null);

            var consumer = new QueueingBasicConsumer(_commandChannel);
            _commandChannel.BasicConsume(CommandFacadeQueueName, false, consumer);

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

        private void SubmitChanges(IBasicProperties basicProperties, string userToken, string dataSourcePath,
            string toUpdateQueueName, string toDeleteQueuName)
        {
            try
            {
                var toUpdateDataSubject = new Subject<Dictionary<string, object>>();
                var toDeleteDataSubject = new Subject<string>();
                var toUpdateQueueNameLocal = toUpdateQueueName != null ? string.Copy(toUpdateQueueName) : null;
                var toDeleteQueueNameLocal = toDeleteQueuName != null ? string.Copy(toDeleteQueuName) : null;

                var changeRecordsEnumerator = toUpdateDataSubject.ToEnumerable();
                var deletedEnumerator = toDeleteDataSubject.ToEnumerable();

                var task1 = Task.Factory.StartNew(
                        () => toUpdateQueueNameLocal != null ? changeRecordsEnumerator.ToArray() : null);

                var task2 =
                    Task.Factory.StartNew(() => toDeleteQueueNameLocal != null ? deletedEnumerator.ToArray() : null);

                //initialise submit changes method work.
                Task.Factory.StartNew(() =>
                {
                    var replyTo = string.Copy(basicProperties.ReplyTo);
                    var corelationId = string.Copy(basicProperties.CorrelationId);
                    var userTokenLocal = string.Copy(userToken);
                    var dataSourcePathLocal = string.Copy(dataSourcePath);
                    
                    var changedRecords = task1.Result;
                    var deleted = task2.Result;

                    _commandFacade.SubmitChanges(dataSourcePathLocal, userTokenLocal, changedRecords, deleted, ri =>
                    {
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = corelationId;
                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(ri));
                    });
                });


                //collect changed records
                if (toUpdateQueueNameLocal != null)
                {
                    var con = new QueueingBasicConsumer(_commandChannel);
                    _commandChannel.BasicConsume(toUpdateQueueNameLocal, false, con);

                    Task.Factory.StartNew(obj =>
                    {
                        var consumer = (QueueingBasicConsumer)obj;

                        while (true)
                        {
                            var ea = consumer.Queue.Dequeue();

                            var memStream = new MemoryStream();
                            var binForm = new BinaryFormatter();
                            memStream.Write(ea.Body, 0, ea.Body.Length);
                            memStream.Seek(0, SeekOrigin.Begin);

                            var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                            if (responce.LastMessage)
                            {
                                toUpdateDataSubject.OnCompleted();
                                break;
                            }

                            foreach (var dictionary in (IEnumerable<Dictionary<string, object>>)responce.Data)
                            {
                                toUpdateDataSubject.OnNext(dictionary);
                            }
                        }
                    }, con);
                }

                //collect record keys to delete
                if (toDeleteQueueNameLocal != null)
                {
                    var con = new QueueingBasicConsumer(_commandChannel);
                    _commandChannel.BasicConsume(toDeleteQueueNameLocal, false, con);

                    Task.Factory.StartNew(obj =>
                    {
                        var consumer = (QueueingBasicConsumer)obj;

                        while (true)
                        {
                            var ea = consumer.Queue.Dequeue();

                            var memStream = new MemoryStream();
                            var binForm = new BinaryFormatter();
                            memStream.Write(ea.Body, 0, ea.Body.Length);
                            memStream.Seek(0, SeekOrigin.Begin);

                            var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                            if (responce.LastMessage)
                            {
                                toDeleteDataSubject.OnCompleted();
                                break;
                            }

                            foreach (var dictionary in (IEnumerable<string>)responce.Data)
                            {
                                toDeleteDataSubject.OnNext(dictionary);
                            }
                        }
                    }, con);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("SubmitChanges failed", ex);
            }
        }

        public void Dispose()
        {
            _task.Dispose();
        }
    }
}
