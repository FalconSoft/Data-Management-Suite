using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
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
        private readonly Dictionary<string, RevisionInfo> _revisionInfos;
        private readonly Dictionary<string, string> _connectionIDictionary; 
        private readonly object _initializeLock = new object();

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
            _revisionInfos = new Dictionary<string, RevisionInfo>();
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
                    SubmitChangesDeleteFinilize(connectionId, dataSourcePath);
                }
                if (_toUpdateSubjects.ContainsKey(dataSourcePath))
                {
                    SubmitChangesChangeRecordFinilize(connectionId, dataSourcePath);
                }
            }
            return base.OnDisconnected();
        }

        public void InitilizeSubmit(string connectionId, string dataSourceInfoPath, string comment, bool isChangeDataNull,
            bool isDeleteDataNull)
        {
            lock (_initializeLock)
            {
                Trace.WriteLine("   Init complete");
                Trace.WriteLine("");
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
                    var deleteEnumerator = _isDeleteDataNull
                        ? null
                        : _toDelteSubjects[_dataSourceInfoPath].ToEnumerable();
                    var changeRecord = _isChangeDataNull ? null : _toUpdateSubjects[_dataSourceInfoPath].ToEnumerable();
                    var deleteToArray = deleteEnumerator != null ? deleteEnumerator.ToArray() : null;
                    var changedRecordsToArray = changeRecord != null ? changeRecord.ToArray() : null;

                    _commandFacade.SubmitChanges(_dataSourceInfoPath, _comment,
                        changedRecordsToArray, deleteToArray,
                        r => _revisionInfos.Add(_dataSourceInfoPath, r),
                        ex => Clients.Client(connectionId.ToString()).OnFail(ex));
                });

                _workingTasks.Add(dataSourceInfoPath, task);
                Clients.Client(connectionId).InitilizeComplete();
            }
        }

        public void SubmitChangesDeleteOnNext(string connectionId, string dataSourceInfoPath, string toDeleteKey)
        {
            LockSubmitChangesDeleteCall(dataSourceInfoPath, toDeleteKey);
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
            Trace.WriteLine("   On next");
            LockSubmitChangesChangeRecordCall(connectionId, dataSourceInfoPath,changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string connectionId, string dataSourceInfoPath)
        {
            Trace.WriteLine("   On Coplete");
            LockSubmitChangesChangeRecordCall(connectionId, dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnFinish(string connectionId, string dataSourceInfoPath, int count)
        {
            Trace.WriteLine("   On Finish");
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
                    _workingTasks[dataSourceInfoPath].Wait();
                Clients.Client(connectionId).OnSuccess(_revisionInfos[dataSourceInfoPath]);

                _revisionInfos.Remove(dataSourceInfoPath);
                _workingTasks.Remove(dataSourceInfoPath);
                _toUpdateSubjects.Remove(dataSourceInfoPath);
                _onUpdateCompleteCall.Remove(dataSourceInfoPath);
                _toUpdateCounter.Remove(dataSourceInfoPath);
                _toUpdateCount.Remove(dataSourceInfoPath);
                _onUpdateNextCall.Remove(dataSourceInfoPath);

                if (_connectionIDictionary.ContainsKey(connectionId))
                    _connectionIDictionary.Remove(connectionId);
                Trace.WriteLine("   Submit Finilized");
                Trace.WriteLine("");
            }
        }

        private void SubmitChangesDeleteFinilize(string connectionId, string dataSourceInfoPath)
        {
            if (_toDelteSubjects.ContainsKey(dataSourceInfoPath) && _onDeleteNextCall[dataSourceInfoPath] && _onDeleteCompleteCall[dataSourceInfoPath] && (_toDeleteCounter[dataSourceInfoPath] == _toDeleteCount[dataSourceInfoPath]))
            {
                _toDelteSubjects[dataSourceInfoPath].OnCompleted();
                if (!_workingTasks[dataSourceInfoPath].IsCompleted)
                    _workingTasks[dataSourceInfoPath].Wait();
                Clients.Client(connectionId).OnSuccess(_revisionInfos[dataSourceInfoPath]);
                
                _revisionInfos.Remove(dataSourceInfoPath);
                _workingTasks.Remove(dataSourceInfoPath);
                _toDelteSubjects.Remove(dataSourceInfoPath);
                _onDeleteCompleteCall.Remove(dataSourceInfoPath);
                _toDeleteCounter.Remove(dataSourceInfoPath);
                _toDeleteCount.Remove(dataSourceInfoPath);

                if (_connectionIDictionary.ContainsKey(connectionId))
                    _connectionIDictionary.Remove(connectionId);
            }
        }

    }
     
}
