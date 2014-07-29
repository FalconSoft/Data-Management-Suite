using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("ICommandFacade")]
    public class CommandsHub : Hub
    {
        private readonly ILogger _logger;

        private readonly ICommandFacade _commandFacade;
        private readonly Dictionary<string,Subject<string>>_toDelteSubjects;
        private readonly Dictionary<string, Subject<Dictionary<string, object>>> _toUpdateSubjects;
        private readonly Dictionary<string,Task> _workingTasks;
        private readonly Dictionary<string, int> _toDeleteCounter;
        private readonly Dictionary<string, int> _toUpdateCounter;
        private readonly Dictionary<string, int> _toDeleteCount;
        private readonly Dictionary<string, int> _toUpdateCount;
        private readonly Dictionary<string, bool> _onDeleteCompleteCall;
        private readonly Dictionary<string, bool> _onUpdateCompleteCall;
        private readonly Dictionary<string, bool> _onUpdateNextCall;
        private readonly Dictionary<string, bool> _onDeleteNextCall;
        
        private readonly object _onUpdateCounterLock = new object();
        private readonly object _onDeleteCounterLock = new object();
        private readonly object _finilizingDictionaryCleanLock = new object();

        public CommandsHub(ICommandFacade commandFacade, ILogger logger)
        {
            _logger = logger;
            
            _toDelteSubjects = new Dictionary<string, Subject<string>>();
            _toUpdateSubjects = new Dictionary<string, Subject<Dictionary<string, object>>>();
            _workingTasks = new Dictionary<string, Task>();
            _toDeleteCounter = new Dictionary<string, int>();
            _toUpdateCounter = new Dictionary<string, int>();
            _toDeleteCount = new Dictionary<string, int>();
            _toUpdateCount = new Dictionary<string, int>();
            _onDeleteCompleteCall = new Dictionary<string, bool>();
            _onUpdateCompleteCall = new Dictionary<string, bool>();
            _onUpdateNextCall = new Dictionary<string, bool>();
            _onDeleteNextCall = new Dictionary<string, bool>();
            _commandFacade = commandFacade;
        }

        public override Task OnDisconnected()
        {
            var connectionId = string.Copy(Context.ConnectionId);
            _logger.Debug("Disconected : " + connectionId);

            if (_toDelteSubjects.ContainsKey(connectionId))
            {
                _toDelteSubjects[connectionId].OnError(new Exception("Client program disconected while transfering data"));
                if (_workingTasks.ContainsKey(connectionId))
                    _workingTasks.Remove(connectionId);
                _toDelteSubjects.Remove(connectionId);
                _onDeleteCompleteCall.Remove(connectionId);
                _toDeleteCounter.Remove(connectionId);
                _toDeleteCount.Remove(connectionId);
                _onDeleteNextCall.Remove(connectionId);
            }

            if (_toUpdateSubjects.ContainsKey(connectionId))
            {
                _toUpdateSubjects[connectionId].OnError(
                    new Exception("Client program disconected while transfering data"));
                if (_workingTasks.ContainsKey(connectionId))
                    _workingTasks.Remove(connectionId);
                _toUpdateSubjects.Remove(connectionId);
                _onUpdateCompleteCall.Remove(connectionId);
                _toUpdateCounter.Remove(connectionId);
                _toUpdateCount.Remove(connectionId);
                _onUpdateNextCall.Remove(connectionId);
            }
            return base.OnDisconnected();
        }

        public void InitilizeSubmit(string connectionId, string dataSourceInfoPath, string userToken,
            bool isChangeDataNull,
            bool isDeleteDataNull)
        {
           _logger.Debug("Submit data start connection Id : " + connectionId);
           
            if (!isDeleteDataNull)
            {
                _toDelteSubjects.Add(connectionId, new Subject<string>());
                _toDeleteCounter.Add(connectionId, 0);
                _toDeleteCount.Add(connectionId, 0);
                _onDeleteCompleteCall.Add(connectionId, false);
                _onDeleteNextCall.Add(connectionId, false);
            }
            if (!isChangeDataNull)
            {
                _toUpdateSubjects.Add(connectionId, new Subject<Dictionary<string, object>>());
                _toUpdateCounter.Add(connectionId, 0);
                _toUpdateCount.Add(connectionId, 0);
                _onUpdateCompleteCall.Add(connectionId, false);
                _onUpdateNextCall.Add(connectionId, false);
            }


            var _dataSourceInfoPath = string.Copy(dataSourceInfoPath);
            var _userToken = string.Copy(userToken);
            var _isDeleteDataNull = isDeleteDataNull;
            var _isChangeDataNull = isChangeDataNull;

            var deleteEnumerator = _isDeleteDataNull ? null : _toDelteSubjects[connectionId].ToEnumerable();
            var changeRecord = _isChangeDataNull ? null : _toUpdateSubjects[connectionId].ToEnumerable();

            var changeRecordsTask = Task<IEnumerable<Dictionary<string, object>>>.Factory.StartNew(() => changeRecord != null ? changeRecord.ToArray() : null); //LoggedInUser.UserToken .ToArray() - BAD FIX 
                var deleteToArrayTask = Task<IEnumerable<string>>.Factory.StartNew(() => deleteEnumerator != null ? deleteEnumerator.ToArray() : null); //TODO .ToArray() - BAD FIX
                
                var task = Task.Factory.StartNew(() =>
                {
                    changeRecordsTask.Wait();
                    deleteToArrayTask.Wait();
                    var changedRecordsToArray = changeRecordsTask.Result;
                    var deleteToArray = deleteToArrayTask.Result;
                    _commandFacade.SubmitChanges(_dataSourceInfoPath, _userToken,
                        changedRecordsToArray, deleteToArray,
                        r => Clients.Client(connectionId).OnSuccess(r),
                        ex => Clients.Client(connectionId).OnFail(ex),
                        (key, msg) => Clients.Client(connectionId).OnNotify(key,msg));
                });
            
            _workingTasks.Add(connectionId, task);
            Clients.Client(connectionId).InitilizeComplete();
        }

        public void SubmitChangesDeleteOnNext(string connectionId, string[] toDeleteKey)
        {
            LockSubmitChangesDeleteCall(connectionId, toDeleteKey);
        }

        public void SubmitChangesDeleteOnComplete(string connectionId)
        {
            LockSubmitChangesDeleteCall(connectionId);
        }

        public void SubmitChangesDeleteOnFinish(string connectionId, int count)
        {
            _toDeleteCount[connectionId] = count;
            SubmitChangesDeleteFinilize(connectionId);
        }

        public void SubmitChangesDeleteOnError(string connectionId, Exception ex)
        {
            _logger.Debug("On delete. Error comes from client: " + connectionId, ex);
            _toDelteSubjects[connectionId].OnError(ex);
        }

        public void SubmitChangesChangeRecordsOnNext(string connectionId, Dictionary<string, object>[] changedRecord)
        {
            LockSubmitChangesChangeRecordCall(connectionId,changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string connectionId)
        {
            LockSubmitChangesChangeRecordCall(connectionId);
        }

        public void SubmitChangesChangeRecordsOnFinish(string connectionId, int count)
        {
            _toUpdateCount[connectionId] = count;
            SubmitChangesChangeRecordFinilize(connectionId);
        }

        public void SubmitChangesChangeRecordsOnError(string connectionId, Exception ex)
        {
            _logger.Debug("On Update changes. Error comes from client: " + connectionId, ex);
            _toUpdateSubjects[connectionId].OnError(ex);
        }


        private void LockSubmitChangesDeleteCall(string connectionId, string[] toDeleteKey = null)
        {
            if (toDeleteKey!=null)
            {
                lock (_onDeleteCounterLock)
                {
                    foreach (var recordKey in toDeleteKey)
                    {
                        _toDelteSubjects[connectionId].OnNext(recordKey);
                        ++_toDeleteCounter[connectionId];
                        _onDeleteNextCall[connectionId] = true;
                        SubmitChangesDeleteFinilize(connectionId);
                    }
                }
            }
            else
            {
                _onDeleteCompleteCall[connectionId] = true;
                SubmitChangesDeleteFinilize(connectionId);
            }
        }


        private void LockSubmitChangesChangeRecordCall(string connectionId,
            Dictionary<string, object> [] changedRecord = null)
        {
            if (changedRecord != null)
            {
                lock (_onUpdateCounterLock)
                {
                    foreach (var dictionary in changedRecord)
                    {
                        _toUpdateSubjects[connectionId].OnNext(dictionary);
                        ++_toUpdateCounter[connectionId];
                        _onUpdateNextCall[connectionId] = true;
                        SubmitChangesChangeRecordFinilize(connectionId);
                    }
                }
            }
            else
            {
                _onUpdateCompleteCall[connectionId] = true;
                SubmitChangesChangeRecordFinilize(connectionId);
            }
        }

        private void SubmitChangesChangeRecordFinilize(string connectionId)
        {
            var connectionIdLocal = string.Copy(connectionId);
            if (_toUpdateSubjects.ContainsKey(connectionIdLocal) &&
                _onUpdateNextCall[connectionIdLocal] &&
                _onUpdateCompleteCall[connectionIdLocal] &&
                (_toUpdateCounter[connectionIdLocal] == _toUpdateCount[connectionIdLocal]) &&
                (_toUpdateCounter[connectionIdLocal] != 0))
            {
                _toUpdateSubjects[connectionIdLocal].OnCompleted();
                _logger.Debug("To update data transfer complete : " + connectionIdLocal);
                
                if (_workingTasks.ContainsKey(connectionIdLocal))
                    _workingTasks.Remove(connectionIdLocal);
                _toUpdateSubjects.Remove(connectionIdLocal);
                _onUpdateCompleteCall.Remove(connectionIdLocal);
                _toUpdateCounter.Remove(connectionIdLocal);
                _toUpdateCount.Remove(connectionIdLocal);
                _onUpdateNextCall.Remove(connectionIdLocal);
            }
        }

        private void SubmitChangesDeleteFinilize(string connectionId)
        {
            var connectionIdLocal = string.Copy(connectionId);
            if (_toDelteSubjects.ContainsKey(connectionIdLocal) &&
                _onDeleteNextCall[connectionIdLocal] &&
                _onDeleteCompleteCall[connectionIdLocal] &&
                (_toDeleteCounter[connectionIdLocal] == _toDeleteCount[connectionIdLocal]) &&
                (_toDeleteCounter[connectionIdLocal] != 0))
            {
                _toDelteSubjects[connectionIdLocal].OnCompleted();
                _logger.Debug("To delete data transfer complete : " + connectionIdLocal);
               
                if (_workingTasks.ContainsKey(connectionIdLocal))
                    _workingTasks.Remove(connectionIdLocal);
                _toDelteSubjects.Remove(connectionIdLocal);
                _onDeleteCompleteCall.Remove(connectionIdLocal);
                _toDeleteCounter.Remove(connectionIdLocal);
                _toDeleteCount.Remove(connectionIdLocal);
                _onDeleteNextCall.Remove(connectionIdLocal);
            }
        }
    }
     
}
