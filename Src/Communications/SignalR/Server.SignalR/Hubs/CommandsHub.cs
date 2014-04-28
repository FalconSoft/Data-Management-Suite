using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
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
        private readonly Dictionary<string, RevisionInfo> _onDeleteRevisionInfos;
        private readonly Dictionary<string, RevisionInfo> _onUpdateRevisionInfo;
        private readonly Dictionary<string, IDisposable> _toDeleteDisposables;
        private readonly Dictionary<string, IDisposable> _toUpdateDisposables;
        private readonly Object _lock = new object();

        public CommandsHub(ICommandFacade commandFacade)
        {
            _toDelteSubjects = new Dictionary<string, Subject<string>>();
            _toUpdateSubjects = new Dictionary<string, Subject<Dictionary<string, object>>>();
            _onDeleteRevisionInfos = new Dictionary<string, RevisionInfo>();
            _onUpdateRevisionInfo = new Dictionary<string, RevisionInfo>();
            _toDeleteDisposables = new Dictionary<string, IDisposable>();
            _toUpdateDisposables = new Dictionary<string, IDisposable>();
            _commandFacade = commandFacade;
        }

        public void InitilizeSubmit(string dataSourceInfoPath, string comment, bool isChangeDataNull,
            bool isDeleteDataNull)
        {
            if (!isDeleteDataNull)
                _toDelteSubjects.Add(dataSourceInfoPath, new Subject<string>());
            if (!isChangeDataNull)
                _toUpdateSubjects.Add(dataSourceInfoPath, new Subject<Dictionary<string, object>>());

            Task.Factory.StartNew(connectionId =>
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
                    r =>
                    {
                        if (!_isDeleteDataNull)
                            _toDelteSubjects.Remove(_dataSourceInfoPath);
                        if (!_isChangeDataNull)
                            _toUpdateSubjects.Remove(_dataSourceInfoPath);
                        Clients.Client(connectionId.ToString()).OnSuccess(r);
                    },
                    ex =>
                    {
                         if (!_isDeleteDataNull)
                            _toDelteSubjects.Remove(_dataSourceInfoPath);
                        if (!_isChangeDataNull)
                            _toUpdateSubjects.Remove(_dataSourceInfoPath);
                        Clients.Client(connectionId.ToString()).OnFail(ex);
                    });
            }, string.Copy(Context.ConnectionId));

            Clients.Caller.InitilizeComplete();
        }

        public void SubmitChangesDeleteOnNext(string dataSourceInfoPath, string toDeleteKey)
        {
            _toDelteSubjects[dataSourceInfoPath].OnNext(toDeleteKey);
        }

        public void SubmitChangesDeleteOnComplete(string dataSourceInfoPath)
        {
            _toDelteSubjects[dataSourceInfoPath].OnCompleted();
        }

        public void SubmitChangesDeleteOnError(string dataSourceInfoPath, Exception ex)
        {
            _toDelteSubjects[dataSourceInfoPath].OnError(ex);
        }

        public void SubmitChangesChangeRecordsOnNext(string dataSourceInfoPath, Dictionary<string, object> changedRecord)
        {
           _toUpdateSubjects[dataSourceInfoPath].OnNext(changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string dataSourceInfoPath)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnCompleted();
        }

        public void SubmitChangesChangeRecordsOnError(string dataSourceInfoPath, Exception ex)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnError(ex);
        }

       
    }
     
}
