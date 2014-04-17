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
        private readonly Dictionary<string,Subject<string>> _toDelteSubjects;
        private readonly Dictionary<string, Subject<Dictionary<string, object>>> _toUpdateSubjects;
        private readonly Dictionary<string, RevisionInfo> _onDeleteRevisionInfos;
        private readonly Dictionary<string, RevisionInfo> _onUpdateRevisionInfo;
        private readonly Dictionary<string, IDisposable> _toDeleteDisposables;
        private readonly Dictionary<string, IDisposable> _toUpdateDisposables;
        private object _toDeleteLock = new object();
        private object _toUpdateLock = new object();

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


        public void SubmitChangesDeleteOnNext(string dataSourceInfoPath, string comment, string toDeleteKey)
        {
            if (!_toDelteSubjects.ContainsKey(dataSourceInfoPath))
            {

                _toDelteSubjects.Add(dataSourceInfoPath, new Subject<string>());

                var disposable = _toDelteSubjects[dataSourceInfoPath].Buffer(TimeSpan.FromMilliseconds(100))
                    .Subscribe(enumerator => _commandFacade.SubmitChanges(dataSourceInfoPath, comment, null,
                        enumerator,
                        r => _onDeleteRevisionInfos[dataSourceInfoPath] = r,
                        ex => Clients.Caller.OnFail(ex)));
                _toDeleteDisposables.Add(dataSourceInfoPath, disposable);

            }
            _toDelteSubjects[dataSourceInfoPath].OnNext(toDeleteKey);
        }

        public void SubmitChangesDeleteOnComplete(string dataSourceInfoPath)
        {
            _toDelteSubjects[dataSourceInfoPath].OnCompleted();
            _toDeleteDisposables[dataSourceInfoPath].Dispose();
            Clients.Caller.OnSuccess(new RevisionInfo());
            //Clients.Caller.OnSuccess(_onDeleteRevisionInfos[dataSourceInfoPath]);
            
            _toDelteSubjects.Remove(dataSourceInfoPath);
            _toDeleteDisposables.Remove(dataSourceInfoPath);
            //_onDeleteRevisionInfos.Remove(dataSourceInfoPath);
        }

        public void SubmitChangesDeleteOnError(string dataSourceInfoPath, Exception ex)
        {
            _toDelteSubjects[dataSourceInfoPath].OnError(ex);
        }

        //*****************************************************************************************************

        public void SubmitChangesChangeRecordsOnNext(string dataSourceInfoPath, string comment,
            Dictionary<string, object> changedRecord)
        {
            if (!_toUpdateSubjects.ContainsKey(dataSourceInfoPath))
            {
                _toUpdateSubjects.Add(dataSourceInfoPath,new Subject<Dictionary<string, object>>());

                var disposable =_toUpdateSubjects[dataSourceInfoPath].Buffer(TimeSpan.FromMilliseconds(100))
                    .Subscribe(enumerator =>
                        _commandFacade.SubmitChanges(dataSourceInfoPath, comment,
                            enumerator, null,
                            r => _onUpdateRevisionInfo[dataSourceInfoPath] = r,
                            ex => Clients.Caller.OnFail(ex)));
                _toUpdateDisposables.Add(dataSourceInfoPath,disposable);

            }
            _toUpdateSubjects[dataSourceInfoPath].OnNext(changedRecord);
        }

        public void SubmitChangesChangeRecordsOnComplete(string dataSourceInfoPath)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnCompleted();
            _toUpdateDisposables[dataSourceInfoPath].Dispose();
            Clients.Caller.OnSuccess(new RevisionInfo());
            //Clients.Caller.OnSuccess(_onUpdateRevisionInfo[dataSourceInfoPath]);

            _toUpdateSubjects.Remove(dataSourceInfoPath);
            _toUpdateDisposables.Remove(dataSourceInfoPath);
            //_onUpdateRevisionInfo.Remove(dataSourceInfoPath);
        }

        public void SubmitChangesChangeRecordsOnError(string dataSourceInfoPath, Exception ex)
        {
            _toUpdateSubjects[dataSourceInfoPath].OnError(ex);
        }
    }
     
}
