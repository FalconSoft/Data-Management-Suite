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
        private readonly string _connectionString;

        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;
        private Action<RevisionInfo> _onSuccessAction;
        private Action<Exception> _onFailedAction;
        private Action _onInitilizeCompleteAction;

        public CommandFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        private void InitialiseConnection(string connectionString)
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

            _proxy.On("InitilizeComplete", () =>
            {
                if (_onInitilizeCompleteAction != null)
                    _onInitilizeCompleteAction();
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

        public void SubmitChanges(string dataSourcePath, string comment,
            IEnumerable<Dictionary<string, object>> changedRecords = null, IEnumerable<string> deleted = null,
            Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null,
            Action<string, string> onValidationError = null)
        {
            _onSuccessAction = onSuccess;
            _onFailedAction = onFail;
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onInitilizeCompleteAction = () => tcs.SetResult(new object());
            
            CheckConnectionToServer();

            _proxy.Invoke("InitilizeSubmit", dataSourcePath, comment, changedRecords==null, deleted==null);
            task.Wait();

            if (deleted != null)
                DeleteServerCall(dataSourcePath, deleted);
            if (changedRecords != null)
                ChangeRecordsServerCall(dataSourcePath, changedRecords);
        }

        private void DeleteServerCall(string dataSourcePath, IEnumerable<string> deleted)
        {
            CheckConnectionToServer();
            try
            {
                var count = 0;
                foreach (var keyToDelete in deleted)
                {
                    _proxy.Invoke("SubmitChangesDeleteOnNext", dataSourcePath, keyToDelete);
                    count++;
                }
                _proxy.Invoke("SubmitChangesDeleteOnFinish", dataSourcePath, count);
                _proxy.Invoke("SubmitChangesDeleteOnComplete",dataSourcePath);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesDeleteOnError",dataSourcePath,ex);
                throw;
            }
        }

        private void ChangeRecordsServerCall(string dataSourcePath, IEnumerable<Dictionary<string, object>> changedRecords)
        {
            CheckConnectionToServer();
            try
            {
                var count = 0;
                foreach (var dataToUpdate in changedRecords)
                {
                    _proxy.Invoke("SubmitChangesChangeRecordsOnNext", dataSourcePath, dataToUpdate);
                    count++;
                }
                _proxy.Invoke("SubmitChangesChangeRecordsOnFinish", dataSourcePath, count);
                _proxy.Invoke("SubmitChangesChangeRecordsOnComplete",dataSourcePath);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesChangeRecordsOnError",dataSourcePath, ex);
                throw;
            }
        }

        private void CheckConnectionToServer()
        {
            if (_connection.State == ConnectionState.Disconnected)
            {
                InitialiseConnection(_connectionString);
            }
            if (!_startConnectionTask.IsCompleted)
                _startConnectionTask.Wait();
        }
    }
}