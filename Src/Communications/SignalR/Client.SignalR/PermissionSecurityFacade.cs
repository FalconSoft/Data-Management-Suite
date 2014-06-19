using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.Data.Management.Client.SignalR
{
    public class PermissionSecurityFacade : IPermissionSecurityFacade
    {
        private readonly string _connectionString;

        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;

        private Action<string> _onMessageResiveAction;

        public PermissionSecurityFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        public Permission GetUserPermissions(string userToken)
        {
            CheckConnectionToServer();

            var tcs = new TaskCompletionSource<Permission>();
            var task = tcs.Task;
            _proxy.Invoke<Permission>("GetUserPermissions", userToken)
                .ContinueWith(t => tcs.SetResult(t.Result.Id == null ? null : t.Result));
            return task.Result;
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken,Action<string> messageAction = null)
        {
            _onMessageResiveAction = messageAction;
            _proxy.Invoke("SaveUserPermissions", _connection.ConnectionId, permissions, targetUserToken, grantedByUserToken);
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            CheckConnectionToServer();
            var tcs = new TaskCompletionSource<AccessLevel>();
            var task = tcs.Task;
            _proxy.Invoke<AccessLevel>("CheckAccess", userToken, urn);
            task.Wait();
            return task.Result;
        }

        private void InitialiseConnection(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("IPermissionSecurityHub");

            _proxy.On<string>("OnMessageAction", revisionInfo =>
            {
                if (_onMessageResiveAction != null)
                    _onMessageResiveAction(revisionInfo);
            });

           _startConnectionTask = _connection.Start();
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

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
