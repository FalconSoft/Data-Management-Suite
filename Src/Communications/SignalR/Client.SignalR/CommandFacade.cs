using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class CommandFacade : ICommandFacade
    {
        private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private readonly Task _startConnectionTask;
        private Action<RevisionInfo> _onSuccessAction;
        private Action<Exception> _onFailedAction; 

        public CommandFacade(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("ICommandFacade");
            _proxy.On<RevisionInfo>("OnSuccess", revisionInfo =>
            {
                if (_onSuccessAction != null)
                    _onSuccessAction(revisionInfo);
            });
            _proxy.On<Exception>("OnFail", ex =>
            {
                if (_onFailedAction != null)
                    _onFailedAction(ex);
            });
            _startConnectionTask = _connection.Start();
        }

        public void Dispose()
        {
            _connection.Stop();
        }

        public void SubmitChanges<T>(string dataSourcePath, string comment, IEnumerable<T> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null,
            Action<string, string> onValidationError = null)
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges(string dataSourcePath, string comment, IEnumerable<Dictionary<string, object>> changedRecords = null, IEnumerable<string> deleted = null,
            Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onValidationError = null)
        {
            _onSuccessAction = onSuccess;
            _onFailedAction = onFail;
            if (deleted != null)
                DeleteServerCall(dataSourcePath, comment, deleted);
            if (changedRecords != null)
                ChangeRecordsServerCall(dataSourcePath, comment, changedRecords);
        }

        private void DeleteServerCall(string dataSourcePath, string comment, IEnumerable<string> deleted = null)
        {
            if (!_startConnectionTask.IsCompleted)
                _startConnectionTask.Wait();
            try
            {
                foreach (var keyToDelete in deleted)
                {
                    _proxy.Invoke("SubmitChangesDeleteOnNext", dataSourcePath, comment, keyToDelete);
                }
                _proxy.Invoke("SubmitChangesDeleteOnComplete",dataSourcePath);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesDeleteOnError",dataSourcePath,ex);
                throw ex;
            }
        }

        private void ChangeRecordsServerCall(string dataSourcePath, string comment,
            IEnumerable<Dictionary<string, object>> changedRecords = null)
        {
            if (!_startConnectionTask.IsCompleted)
                _startConnectionTask.Wait();
            try
            {
                foreach (var dataToUpdate in changedRecords)
                {
                    _proxy.Invoke("SubmitChangesChangeRecordsOnNext", dataSourcePath, comment, dataToUpdate);
                }
                _proxy.Invoke("SubmitChangesChangeRecordsOnComplete",dataSourcePath);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesChangeRecordsOnError",dataSourcePath, ex);
                throw ex;
            }
        }
    }
}