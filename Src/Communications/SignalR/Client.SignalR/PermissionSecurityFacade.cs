using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private bool _allowToRestoreConnection;

        private Action<string> _onMessageResiveAction;
        private readonly Subject<Dictionary<string,AccessLevel>> _permissionChangedSubject = new Subject<Dictionary<string, AccessLevel>>(); 

        public PermissionSecurityFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        public Permission GetUserPermissions(string userToken)
        {
            if (userToken == null)
                return null;

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
            if (targetUserToken != null)
            {
                _proxy.Invoke("SaveUserPermissions", _connection.ConnectionId, permissions, targetUserToken,
                    grantedByUserToken);
            } else if (messageAction != null)
            {
                messageAction("Target user id do not input");
            }
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            CheckConnectionToServer();
         
            var tcs = new TaskCompletionSource<AccessLevel>();
            var task = tcs.Task;
            _proxy.Invoke<AccessLevel>("CheckAccess", userToken, urn)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    tcs.SetResult(t.Result);
                });
            return task.Result;
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            var observable = Observable.Create<Dictionary<string, AccessLevel>>(observer =>
            {
                var disposable = _permissionChangedSubject.Subscribe(observer.OnNext);

                var action = new Action(() =>
                {
                    CheckConnectionToServer();
                    if (_allowToRestoreConnection)
                    {
                        _allowToRestoreConnection = false;
                        _proxy.Invoke("GetPermissionChanged", _connection.ConnectionId, userToken);
                    }

                });

                var keepAliveTimer = new Timer(OnKeepAliveTick, action, 5000, 3000);

                return Disposable.Create(() =>
                {
                    disposable.Dispose();
                    keepAliveTimer.Dispose();

                });
            });

            CheckConnectionToServer();

            _proxy.Invoke("GetPermissionChanged", _connection.ConnectionId, userToken);

            return observable;
        }

        private void OnKeepAliveTick(object onTickAction)
        {
            var action = onTickAction as Action;
            if (action != null)
                action();
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

            _proxy.On<Dictionary<string, AccessLevel>>("GetPermissionChangedOnNext",
                dictionary => _permissionChangedSubject.OnNext(dictionary));

           _startConnectionTask = _connection.Start();
        }

        private void CheckConnectionToServer()
        {
            if (_connection.State == ConnectionState.Disconnected)
            {
                _allowToRestoreConnection = true;
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
