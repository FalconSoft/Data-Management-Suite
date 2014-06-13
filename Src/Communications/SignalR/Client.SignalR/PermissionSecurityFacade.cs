using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private Action _onCompleteAction;

        public PermissionSecurityFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        public IEnumerable<Permission> GetUserPermissions(string userToken)
        {
            CheckConnectionToServer();

            var tcs = new TaskCompletionSource<IEnumerable<Permission>>();
            var task = tcs.Task;
            _proxy.Invoke<Permission[]>("GetUserPermissions", userToken)
                .ContinueWith(t => tcs.SetResult(t.Result));

            return task.Result;
        }

        public void SaveUserPermissions(IEnumerable<Permission> permissions, string targetUserToken, string grantedByUserToken,Action<string> messageAction = null)
        {
            var are = new AutoResetEvent(false);
            _onCompleteAction = ()=> are.Set();
            _onMessageResiveAction = messageAction;
            _proxy.Invoke("SaveUserPermissions", permissions.ToList(), targetUserToken, grantedByUserToken);
        }

        private void InitialiseConnection(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("IPermissionSecurityFacade");

            _proxy.On<string>("MessageAction", revisionInfo =>
            {
                if (_onMessageResiveAction != null)
                    _onMessageResiveAction(revisionInfo);
            });

            _proxy.On("OnComplete", ex =>
            {
                if (_onCompleteAction != null)
                    _onCompleteAction();
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
    }
}
