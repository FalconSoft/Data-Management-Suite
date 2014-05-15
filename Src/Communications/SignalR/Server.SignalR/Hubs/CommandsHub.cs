using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("ICommandFacade")]
    public class CommandsHub : Hub
    {
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
        private readonly Dictionary<string, string> _connectionIDictionary;

        public CommandsHub(ICommandFacade commandFacade)
        {
            _connectionIDictionary = new Dictionary<string, string>();
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
            if (_connectionIDictionary.ContainsKey(connectionId))
            {
                var dataSourcePath = _connectionIDictionary[connectionId];

                _connectionIDictionary.Remove(connectionId);

                if (_toDelteSubjects.ContainsKey(dataSourcePath))
                {
                    if (_workingTasks.ContainsKey(dataSourcePath))
                        _workingTasks.Remove(dataSourcePath);
                    _toDelteSubjects.Remove(dataSourcePath);
                    _onDeleteCompleteCall.Remove(dataSourcePath);
                    _toDeleteCounter.Remove(dataSourcePath);
                    _toDeleteCount.Remove(dataSourcePath);
                    _onDeleteNextCall.Remove(dataSourcePath);

                    if (_connectionIDictionary.ContainsKey(connectionId))
                        _connectionIDictionary.Remove(connectionId);
                }

                if (_toUpdateSubjects.ContainsKey(dataSourcePath))
                {
                    if (_workingTasks.ContainsKey(dataSourcePath))
                        _workingTasks.Remove(dataSourcePath);
                    _toUpdateSubjects.Remove(dataSourcePath);
                    _onUpdateCompleteCall.Remove(dataSourcePath);
                    _toUpdateCounter.Remove(dataSourcePath);
                    _toUpdateCount.Remove(dataSourcePath);
                    _onUpdateNextCall.Remove(dataSourcePath);

                    if (_connectionIDictionary.ContainsKey(connectionId))
                        _connectionIDictionary.Remove(connectionId);
                }
            }
            return base.OnDisconnected();
        }

        public void InitilizeSubmit(string connectionId, string dataSourceInfoPath, string comment, bool isChangeDataNull,
            bool isDeleteDataNull)
        {
           _connectionIDictionary.Add(connectionId, dataSourceInfoPath);
                if (!isDeleteDataNull)
                {
                    _toDelteSubjects.Add(dataSourceInfoPath, new Subject<string>());
                    _toDeleteCounter.Add(dataSourceInfoPath, 0);
                    _toDeleteCount.Add(dataSourceInfoPath, 0);
                    _onDeleteCompleteCall.Add(dataSourceInfoPath, false);
                    _onDeleteNextCall.Add(dataSourceInfoPath, false);
                }
                if (!isChangeDataNull)
                {
                    _toUpdateSubjects.Add(dataSourceInfoPath, new Subject<Dictionary<string, object>>());
                    _toUpdateCounter.Add(dataSourceInfoPath, 0);
                    _toUpdateCount.Add(dataSourceInfoPath, 0);
                    _onUpdateCompleteCall.Add(dataSourceInfoPath, false);
                    _onUpdateNextCall.Add(dataSourceInfoPath, false);
                }

                var task = Task.Factory.StartNew(()=>
                {
                    var _dataSourceInfoPath = string.Copy(dataSourceInfoPath);
                    var _comment = string.Copy(comment);
                    var _isDeleteDataNull = isDeleteDataNull;
                    var _isChangeDataNull = isChangeDataNull;
                    var deleteEnumerator = _isDeleteDataNull ? null : _toDelteSubjects[_dataSourceInfoPath].ToEnumerable();
                    var changeRecord = _isChangeDataNull ? null : _toUpdateSubjects[_dataSourceInfoPath].ToEnumerable();
                    var deleteToArray = deleteEnumerator != null ? deleteEnumerator.ToArray() : null;
                    var changedRecordsToArray = changeRecord != null ? changeRecord.ToArray() : null;

                    _commandFacade.SubmitChanges(_dataSourceInfoPath, _comment,
                        changedRecordsToArray, deleteToArray,
                        r => Clients.Client(connectionId).OnSuccess(r),
                        ex => Clients.Client(connectionId).OnFail(ex));
                });

                _workingTasks.Add(dataSourceInfoPath, task);
                Clients.Client(connectionId).InitilizeComplete();
        }

        public void SubmitChangesDeleteOnNext(string connectionId, string dataSourceInfoPath, string toDeleteKey)
        {
            LockSubmitChangesDeleteCall(connectionId, dataSourceInfoPath, toDeleteKey);
        }

        public void SubmitChangesDeleteOnComplete(string connectionId, string dataSourceInfoPath)
        {
            LockSubmitChangesDeleteCall(connectionId, dataSourceInfoPath);
        }

        public void SubmitChangesDeleteOnFinish(string connectionId, string dataSourceInfoPath, int count)
        {
            _toDeleteCount[dataSourceInfoPath] = count;
            SubmitChangesDeleteFinilize(connectionId, dataSourceInfoPath);
        }

        public void SubmitChangesDeleteOnError(string dataSourceInfoPath, Exception ex)
        {
            _toDelteSubjects[dataSourceInfoPath].OnError(ex);
        }

        public void SubmitChangesChangeRecordsOnNext(string connectionId, string dataSourceInfoPath, Dictionary<string, object> changedRecord)
        {
            LockSubmitChangesChangeRecordCall(connectionId, dataSourceInfoPath,changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string connectionId, string dataSourceInfoPath)
        {
            LockSubmitChangesChangeRecordCall(connectionId, dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnFinish(string connectionId, string dataSourceInfoPath, int count)
        {
            _toUpdateCount[dataSourceInfoPath] = count;
            SubmitChangesChangeRecordFinilize(connectionId, dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnError(string dataSourceInfoPath, Exception ex)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnError(ex);
        }


        private void LockSubmitChangesDeleteCall(string connectionId, string dataSourceInfoPath, string toDeleteKey = null)
        {
            if (toDeleteKey!=null)
            {
                _toDelteSubjects[dataSourceInfoPath].OnNext(toDeleteKey);
                _toDeleteCounter[dataSourceInfoPath]++;
                _onDeleteNextCall[dataSourceInfoPath] = true;
                SubmitChangesDeleteFinilize(connectionId, dataSourceInfoPath);
            }
            else
            {
                _onDeleteCompleteCall[dataSourceInfoPath] = true;
                SubmitChangesDeleteFinilize(connectionId, dataSourceInfoPath);
            }
        }


        private void LockSubmitChangesChangeRecordCall(string connectionId, string dataSourceInfoPath,
            Dictionary<string, object> changedRecord = null)
        {
            if (changedRecord != null)
            {
                _toUpdateSubjects[dataSourceInfoPath].OnNext(changedRecord);
                _toUpdateCounter[dataSourceInfoPath]++;
                _onUpdateNextCall[dataSourceInfoPath] = true;
                SubmitChangesChangeRecordFinilize(connectionId, dataSourceInfoPath);

            }
            else
            {
                _onUpdateCompleteCall[dataSourceInfoPath] = true;
                SubmitChangesChangeRecordFinilize(connectionId, dataSourceInfoPath);
            }
        }

        private void SubmitChangesChangeRecordFinilize(string connectionId, string dataSourceInfoPath)
        {
            if (_toUpdateSubjects.ContainsKey(dataSourceInfoPath) && _onUpdateNextCall[dataSourceInfoPath] && _onUpdateCompleteCall[dataSourceInfoPath] && (_toUpdateCounter[dataSourceInfoPath] == _toUpdateCount[dataSourceInfoPath]))
            {
                _toUpdateSubjects[dataSourceInfoPath].OnCompleted();
                if (!_workingTasks[dataSourceInfoPath].IsCompleted)
                    _workingTasks[dataSourceInfoPath].ContinueWith(t =>
                    {
                        _workingTasks.Remove(dataSourceInfoPath);
                        _toUpdateSubjects.Remove(dataSourceInfoPath);
                        _onUpdateCompleteCall.Remove(dataSourceInfoPath);
                        _toUpdateCounter.Remove(dataSourceInfoPath);
                        _toUpdateCount.Remove(dataSourceInfoPath);
                        _onUpdateNextCall.Remove(dataSourceInfoPath);

                        if (_connectionIDictionary.ContainsKey(connectionId))
                            _connectionIDictionary.Remove(connectionId);
                    });
            }
        }

        private void SubmitChangesDeleteFinilize(string connectionId, string dataSourceInfoPath)
        {
            if (_toDelteSubjects.ContainsKey(dataSourceInfoPath) && _onDeleteNextCall[dataSourceInfoPath] && _onDeleteCompleteCall[dataSourceInfoPath] && (_toDeleteCounter[dataSourceInfoPath] == _toDeleteCount[dataSourceInfoPath]))
            {
                _toDelteSubjects[dataSourceInfoPath].OnCompleted();
                if (!_workingTasks[dataSourceInfoPath].IsCompleted)
                    _workingTasks[dataSourceInfoPath].ContinueWith(t =>
                    {
                        _workingTasks.Remove(dataSourceInfoPath);
                        _toDelteSubjects.Remove(dataSourceInfoPath);
                        _onDeleteCompleteCall.Remove(dataSourceInfoPath);
                        _toDeleteCounter.Remove(dataSourceInfoPath);
                        _toDeleteCount.Remove(dataSourceInfoPath);
                        _onDeleteNextCall.Remove(dataSourceInfoPath);
                        if (_connectionIDictionary.ContainsKey(connectionId))
                            _connectionIDictionary.Remove(connectionId);
                    });
            }
        }

    }
     
}
