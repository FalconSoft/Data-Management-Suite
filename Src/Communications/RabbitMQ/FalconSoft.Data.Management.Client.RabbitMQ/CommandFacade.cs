using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal class CommandFacade : RabbitMQFacadeBase, ICommandFacade
    {
        private const int TimeOut = 2000;
        private const string CommandFacadeQueueName = "CommandFacadeRPC";

        public CommandFacade(string hostName, string userName, string password):base(hostName,userName,password)
        {
           InitializeConnection(CommandFacadeQueueName);

           KeepAliveAction = () => InitializeConnection(CommandFacadeQueueName);
        }

        public void SubmitChanges<T>(string dataSourcePath, string userToken, IEnumerable<T> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges(string dataSourcePath, string userToken, IEnumerable<Dictionary<string, object>> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            string toUpdateQueueName = null;
            string toDeleteQueueName = null;

            try
            {
                InitializeConnection(CommandFacadeQueueName);

                if (changedRecords != null)
                {
                    toUpdateQueueName = Guid.NewGuid().ToString();
                    CommandChannel.QueueDeclare(toUpdateQueueName, false, false, false, null);
                }

                if (deleted != null)
                {
                    toDeleteQueueName = Guid.NewGuid().ToString();
                    CommandChannel.QueueDeclare(toDeleteQueueName, false, false, false, null);
                }

                var errorNotificationQueueName = CommandChannel.QueueDeclare().QueueName;

                var message = new MethodArgs
                {
                    MethodName = "SubmitChanges",
                    UserToken = userToken,
                    MethodsArgs = new object[] {dataSourcePath, toUpdateQueueName, toDeleteQueueName, errorNotificationQueueName}
                };

                var replyTo = CommandChannel.QueueDeclare().QueueName;

                var correlationId = Guid.NewGuid().ToString();

                var props = CommandChannel.CreateBasicProperties();
                props.ReplyTo = replyTo;
                props.CorrelationId = correlationId;
                props.SetPersistent(true);

                var onFailConsumer = new QueueingBasicConsumer(CommandChannel);
                CommandChannel.BasicConsume(errorNotificationQueueName, false, onFailConsumer);

                var consumer = new QueueingBasicConsumer(CommandChannel);
                CommandChannel.BasicConsume(replyTo, false, consumer);

                CommandChannel.BasicPublish("", CommandFacadeQueueName, props, BinaryConverter.CastToBytes(message));

                if (changedRecords != null)
                {
                    var changeRecordsList = new List<Dictionary<string, object>>();
                    var data = new RabbitMQResponce();
                    foreach (var changedRecord in changedRecords)
                    {
                        changeRecordsList.Add(changedRecord);
                        if (changeRecordsList.Count == 100)
                        {
                            data.Id++;
                            data.Data = changeRecordsList;

                            var bf = new BinaryFormatter();
                            var ms = new MemoryStream();

                            bf.Serialize(ms, data);

                            var messageBytes = ms.ToArray();

                            CommandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);

                            changeRecordsList.Clear();
                        }
                    }

                    if (changeRecordsList.Count > 0)
                    {
                        data.Id++;
                        data.Data = changeRecordsList;

                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        CommandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);

                        changeRecordsList.Clear();

                        data.LastMessage = true;

                        bf = new BinaryFormatter();
                        ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        messageBytes = ms.ToArray();
                        CommandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);
                    }
                    else
                    {
                        data.LastMessage = true;
                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        CommandChannel.BasicPublish("", toUpdateQueueName, null, messageBytes);
                    }
                }

                if (deleted != null)
                {
                    var deletedList = new List<string>();

                    var data = new RabbitMQResponce();

                    foreach (var recordKey in deleted)
                    {
                        deletedList.Add(recordKey);
                        if (deletedList.Count == 100)
                        {
                            data.Id++;
                            data.Data = deletedList;

                            var bf = new BinaryFormatter();
                            var ms = new MemoryStream();

                            bf.Serialize(ms, data);

                            var messageBytes = ms.ToArray();

                            CommandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);

                            deletedList.Clear();
                        }
                    }

                    if (deletedList.Count > 0)
                    {
                        data.Id++;
                        data.Data = deletedList;

                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        CommandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);

                        deletedList.Clear();

                        data.LastMessage = true;

                        bf = new BinaryFormatter();
                        ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        messageBytes = ms.ToArray();
                        CommandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);
                    }
                    else
                    {
                        data.LastMessage = true;
                        var bf = new BinaryFormatter();
                        var ms = new MemoryStream();

                        bf.Serialize(ms, data);

                        var messageBytes = ms.ToArray();

                        CommandChannel.BasicPublish("", toDeleteQueueName, null, messageBytes);
                    }
                }

                if (onNotifcation != null)
                {
                    onNotifcation("Some text", "Transfer complete");
                }

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue();

                        var ri = BinaryConverter.CastTo<RevisionInfo>(ea.Body);

                        if (onSuccess != null)
                            onSuccess(ri);

                        break;
                    }
                });

                var waitForResult = true;

                Task.Factory.StartNew(() =>
                {
                    while (waitForResult)
                    {
                        BasicDeliverEventArgs ea;
                        
                        if(!consumer.Queue.Dequeue(6000, out ea))
                            continue;
                        
                        var ri = BinaryConverter.CastTo<RevisionInfo>(ea.Body);

                        if (onSuccess != null)
                            onSuccess(ri);

                        waitForResult = false;
                        break;
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    while (waitForResult)
                    {
                        BasicDeliverEventArgs ea;
                        
                        if (!onFailConsumer.Queue.Dequeue(6000, out ea))
                            continue;

                        var ri = BinaryConverter.CastTo<Exception>(ea.Body);

                        if (onFail != null)
                            onFail(ri);

                        waitForResult = false;
                        break;
                    }
                });
            }
            catch (Exception exception)
            {
                if (onFail != null)
                    onFail(exception);
            }
        }

        public void Dispose()
        {
            
        }

        public new void Close()
        {
            base.Close();
        }

    }
}
