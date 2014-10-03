using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
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
                        SubmitChanges(basicProperties, message.UserToken, message.MethodsArgs[0] as string, message.MethodsArgs[1] as string, message.MethodsArgs[2] as string, message.MethodsArgs[3] as string);
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
                    //_logger.Debug(DateTime.Now + " Command Broker. InitializeConnection starts");

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
            string toUpdateQueueName, string toDeleteQueuName, string errorNotificationQueueName)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _logger.Debug(DateTime.Now + " Command Broker. SubmitChanges starts");

                    var toUpdateDataSubject = new ProducerConsumerQueue<Dictionary<string, object>>(100);
                    var toDeleteDataSubject = new ProducerConsumerQueue<string>(100);

                    var toUpdateQueueNameLocal = toUpdateQueueName;
                    var toDeleteQueueNameLocal = toDeleteQueuName;


                    //initialise submit changes method work.

                    //collect changed records
                    if (toUpdateQueueNameLocal != null)
                    {
                        var con = new QueueingBasicConsumer(_commandChannel);
                        _commandChannel.BasicConsume(toUpdateQueueNameLocal, false, con);

                        Task.Factory.StartNew(() => ConsumerDataToSubject(con, toUpdateDataSubject), _cts.Token)
                            .ContinueWith(t => toUpdateDataSubject.OnCompleted());
                    }

                    //collect record keys to delete
                    if (toDeleteQueueNameLocal != null)
                    {
                        var con = new QueueingBasicConsumer(_commandChannel);
                        _commandChannel.BasicConsume(toDeleteQueueNameLocal, false, con);

                        Task.Factory.StartNew(() => ConsumerDataToSubject(con, toDeleteDataSubject), _cts.Token)
                            .ContinueWith(t => toDeleteDataSubject.OnCompleted());
                    }

                    var replyTo = basicProperties.ReplyTo;
                    var corelationId = basicProperties.CorrelationId;
                    var userTokenLocal = userToken;
                    var dataSourcePathLocal = dataSourcePath;

                    _commandFacade.SubmitChanges(dataSourcePathLocal, userTokenLocal, toUpdateQueueNameLocal != null ? toUpdateDataSubject : null,
                        toDeleteQueueNameLocal != null ? toDeleteDataSubject : null, ri =>
                    {
                        toUpdateDataSubject.Dispose();
                        toDeleteDataSubject.Dispose();
                        var props = _commandChannel.CreateBasicProperties();
                        props.CorrelationId = corelationId;
                        _commandChannel.BasicPublish("", replyTo, props, BinaryConverter.CastToBytes(ri));
                    }, ex =>
                    {
                        _commandChannel.BasicPublish("", errorNotificationQueueName, null, BinaryConverter.CastToBytes(ex));

                        toUpdateDataSubject.Dispose();
                        toDeleteDataSubject.Dispose();
                    });
                }
                catch (Exception ex)
                {
                    _logger.Debug(DateTime.Now + " SubmitChanges failed", ex);
                    if (_connection.IsOpen)
                        _commandChannel.BasicPublish("", errorNotificationQueueName, null, BinaryConverter.CastToBytes(ex));
                    throw;
                }
            }, _cts.Token);
        }

        private void ConsumerDataToSubject<T>(QueueingBasicConsumer consumer, IObserver<T> subject)
        {
            while (true)
            {
                var ea = consumer.Queue.Dequeue();

                using (var memStream = new MemoryStream())
                {
                    var binForm = new BinaryFormatter();
                    memStream.Write(ea.Body, 0, ea.Body.Length);
                    memStream.Seek(0, SeekOrigin.Begin);

                    var responce = (RabbitMQResponce)binForm.Deserialize(memStream);

                    if (responce.LastMessage)
                    {
                        break;
                    }

                    var data = (List<T>)responce.Data;

                    foreach (var dictionary in data)
                    {
                        subject.OnNext(dictionary);
                    }

                }
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
