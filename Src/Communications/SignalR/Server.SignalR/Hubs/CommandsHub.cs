using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, int> _counter;
        private readonly Dictionary<string, int> _count;
        private readonly Dictionary<string, bool> _onCompleteCalll;
        private readonly Dictionary<string, RevisionInfo> _revisionInfos;

        public CommandsHub(ICommandFacade commandFacade)
        {
            _toDelteSubjects = new Dictionary<string, Subject<string>>();
            _toUpdateSubjects = new Dictionary<string, Subject<Dictionary<string, object>>>();
            _workingTasks = new Dictionary<string, Task>();
            _counter = new Dictionary<string, int>();
            _count = new Dictionary<string, int>();
            _onCompleteCalll = new Dictionary<string, bool>();
            _revisionInfos = new Dictionary<string, RevisionInfo>();
            _commandFacade = commandFacade;
        }

        public void InitilizeSubmit(string dataSourceInfoPath, string comment, bool isChangeDataNull,
            bool isDeleteDataNull)
        {
            if (!isDeleteDataNull)
                _toDelteSubjects.Add(dataSourceInfoPath, new Subject<string>());
            if (!isChangeDataNull)
                _toUpdateSubjects.Add(dataSourceInfoPath, new Subject<Dictionary<string, object>>());

            _counter.Add(dataSourceInfoPath, 0);
            _count.Add(dataSourceInfoPath, 0);
            _onCompleteCalll.Add(dataSourceInfoPath, false);
           var task = Task.Factory.StartNew(connectionId =>
            {
                var _dataSourceInfoPath = string.Copy(dataSourceInfoPath);
                var _comment = string.Copy(comment);
                var _isDeleteDataNull = isDeleteDataNull;
                var _isChangeDataNull = isChangeDataNull;
                var deleteEnumerator = _isDeleteDataNull ? null : _toDelteSubjects[_dataSourceInfoPath].ToEnumerable();
                var changeRecord = _isChangeDataNull ? null : _toUpdateSubjects[_dataSourceInfoPath].ToEnumerable();
                var deleteToArray = deleteEnumerator!=null ? deleteEnumerator.ToArray() : null;
                var changedRecordsToArray = changeRecord != null ? changeRecord.ToArray() : null;

                _commandFacade.SubmitChanges(_dataSourceInfoPath, _comment,
                    changedRecordsToArray, deleteToArray,
                    r => _revisionInfos.Add(_dataSourceInfoPath,r),
                    ex => Clients.Client(connectionId.ToString()).OnFail(ex));
            }, string.Copy(Context.ConnectionId));

            _workingTasks.Add(dataSourceInfoPath,task);
            Clients.Caller.InitilizeComplete();
        }

        public void SubmitChangesDeleteOnNext(string dataSourceInfoPath, string toDeleteKey)
        {
            LockSubmitChangesDeleteCall(dataSourceInfoPath, toDeleteKey);
        }

        public void SubmitChangesDeleteOnComplete(string dataSourceInfoPath)
        {
            LockSubmitChangesDeleteCall(dataSourceInfoPath);
        }

        public void SubmitChangesDeleteOnFinish(string dataSourceInfoPath, int count)
        {
            _count[dataSourceInfoPath] = count;
            SubmitChangesDeleteFinilize(dataSourceInfoPath);
        }

        public void SubmitChangesDeleteOnError(string dataSourceInfoPath, Exception ex)
        {
            _toDelteSubjects[dataSourceInfoPath].OnError(ex);
        }

        public void SubmitChangesChangeRecordsOnNext(string dataSourceInfoPath, Dictionary<string, object> changedRecord)
        {
            LockSubmitChangesChangeRecordCall(dataSourceInfoPath,changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string dataSourceInfoPath)
        {
            LockSubmitChangesChangeRecordCall(dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnFinish(string dataSourceInfoPath, int count)
        {
            _count[dataSourceInfoPath] = count;
            SubmitChangesChangeRecordFinilize(dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnError(string dataSourceInfoPath, Exception ex)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnError(ex);
        }

       
        private void LockSubmitChangesDeleteCall(string dataSourceInfoPath,  string toDeleteKey=null)
        {
            if (toDeleteKey!=null)
            {
                _toDelteSubjects[dataSourceInfoPath].OnNext(toDeleteKey);
                _counter[dataSourceInfoPath]++;
                SubmitChangesDeleteFinilize(dataSourceInfoPath);
            }
            else
            {
                _onCompleteCalll[dataSourceInfoPath] = true;
                SubmitChangesDeleteFinilize(dataSourceInfoPath);
            }
        }


        private void LockSubmitChangesChangeRecordCall(string dataSourceInfoPath,
            Dictionary<string, object> changedRecord = null)
        {
            if (changedRecord != null)
            {

                _toUpdateSubjects[dataSourceInfoPath].OnNext(changedRecord);
                _counter[dataSourceInfoPath]++;
                SubmitChangesChangeRecordFinilize(dataSourceInfoPath);

            }
            else
            {
                _onCompleteCalll[dataSourceInfoPath] = true;
                SubmitChangesChangeRecordFinilize(dataSourceInfoPath);
            }
        }

        private void SubmitChangesChangeRecordFinilize(string dataSourceInfoPath)
        {
            if (_onCompleteCalll[dataSourceInfoPath] && (_counter[dataSourceInfoPath] == _count[dataSourceInfoPath]))
            {
                _toUpdateSubjects[dataSourceInfoPath].OnCompleted();
                if (!_workingTasks[dataSourceInfoPath].IsCompleted)
                    _workingTasks[dataSourceInfoPath].Wait();
                Clients.Caller.OnSuccess(_revisionInfos[dataSourceInfoPath]);

                _revisionInfos.Remove(dataSourceInfoPath);
                _workingTasks.Remove(dataSourceInfoPath);
                _toUpdateSubjects.Remove(dataSourceInfoPath);
                _onCompleteCalll.Remove(dataSourceInfoPath);
                _counter.Remove(dataSourceInfoPath);
                _count.Remove(dataSourceInfoPath);
            }
        }

        private void SubmitChangesDeleteFinilize(string dataSourceInfoPath)
        {
            if (_onCompleteCalll[dataSourceInfoPath] && (_counter[dataSourceInfoPath] == _count[dataSourceInfoPath]))
            {
                _toDelteSubjects[dataSourceInfoPath].OnCompleted();
                if (!_workingTasks[dataSourceInfoPath].IsCompleted)
                    _workingTasks[dataSourceInfoPath].Wait();
                Clients.Caller.OnSuccess(_revisionInfos[dataSourceInfoPath]);
                
                _revisionInfos.Remove(dataSourceInfoPath);
                _workingTasks.Remove(dataSourceInfoPath);
                _toDelteSubjects.Remove(dataSourceInfoPath);
                _onCompleteCalll.Remove(dataSourceInfoPath);
                _counter.Remove(dataSourceInfoPath);
                _count.Remove(dataSourceInfoPath);
            }
        }

    }
     
}
