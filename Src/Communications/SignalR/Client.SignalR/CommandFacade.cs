using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Server.Common;
using FalconSoft.Data.Server.Common.Facade;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.Data.Server.Client.SignalR
{
    internal class CommandFacade : ICommandFacade
    {
        private const int Limit = 100;

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
            Action<string, string> onNotification = null) { }

        public void SubmitChanges(string dataSourcePath, string comment,
            IEnumerable<Dictionary<string, object>> changedRecords = null, IEnumerable<string> deleted = null,
            Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null,
            Action<string, string> onNotification = null)
        {
            if (deleted != null || changedRecords != null)
            {

                _onSuccessAction = onSuccess;
                _onFailedAction = onFail;
                var are = new AutoResetEvent(false);

                _onInitilizeCompleteAction = () => are.Set();

                CheckConnectionToServer();

                _proxy.Invoke("InitilizeSubmit", _connection.ConnectionId, dataSourcePath, comment,
                    changedRecords == null, deleted == null);
                are.WaitOne();
                if (onNotification != null)
                {
                    onNotification("Info", "Finished Transfering");
                }

                if (deleted != null)
                    DeleteServerCall(_connection.ConnectionId, deleted);
                if (changedRecords != null)
                    ChangeRecordsServerCall(_connection.ConnectionId, changedRecords);

            }
        }

        private void DeleteServerCall(string connectionId, IEnumerable<string> deleted)
        {
            CheckConnectionToServer();
            try
            {
                var count = 0;
                var counter = 0;
                var list = new List<string>();
                foreach (var keyToDelete in deleted)
                {
                    ++counter;
                    list.Add(keyToDelete);
                    if (counter == Limit)
                    {
                        counter = 0;
                        _proxy.Invoke("SubmitChangesDeleteOnNext", connectionId, list.ToArray());
                        list.Clear();
                    }
                    ++count;
                }
                if (counter != 0)
                {
                    _proxy.Invoke("SubmitChangesDeleteOnNext", connectionId, list.ToArray());
                    list.Clear();
                }
                _proxy.Invoke("SubmitChangesDeleteOnFinish", connectionId, count);
                _proxy.Invoke("SubmitChangesDeleteOnComplete", connectionId);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesDeleteOnError", connectionId, ex);
                throw;
            }
        }

        private void ChangeRecordsServerCall(string connectionId, IEnumerable<Dictionary<string, object>> changedRecords)
        {
            CheckConnectionToServer();
            try
            {
                var count = 0;
                var counter = 0;
                var list = new List<Dictionary<string, object>>();
                foreach (var dataToUpdate in changedRecords)
                {
                    ++counter;
                    list.Add(dataToUpdate);
                    if (counter == Limit)
                    {
                        counter = 0;
                        _proxy.Invoke("SubmitChangesChangeRecordsOnNext", connectionId, list.ToArray());
                        list.Clear();
                    }
                    ++count;
                }
                if (counter != 0)
                {
                    _proxy.Invoke("SubmitChangesChangeRecordsOnNext", connectionId, list.ToArray());
                    list.Clear();
                }
                _proxy.Invoke("SubmitChangesChangeRecordsOnFinish", connectionId, count);
                _proxy.Invoke("SubmitChangesChangeRecordsOnComplete", connectionId);
            }
            catch (Exception ex)
            {
                _proxy.Invoke("SubmitChangesChangeRecordsOnError", connectionId, ex);
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