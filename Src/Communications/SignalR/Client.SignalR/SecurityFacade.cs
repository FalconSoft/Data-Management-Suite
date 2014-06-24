using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.Data.Management.Client.SignalR
{
    internal class SecurityFacade : ISecurityFacade
    {
        private readonly string _connectionString;
        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;
        private Action<string> _onCompleteAction;

        public SecurityFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        private void InitialiseConnection(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("ISecurityFacade");

            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
            _connection.Closed += OnClosed;

            _proxy.On<string>("OnComplete", userToken =>
            {
                if (_onCompleteAction != null)
                    _onCompleteAction(userToken);
            });

            _proxy.On<string, string>("ErrorMessageHandledAction", (methodName, errorMessage) =>
            {
                if (ErrorMessageHandledAction != null)
                {
                    ErrorMessageHandledAction(methodName, errorMessage);
                }
                Trace.WriteLine(string.Format("MethodName : {0}     Error Message : {1}", methodName, errorMessage));
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

        private void OnClosed()
        {
            //throw new NotImplementedException();
        }

        private void OnReconnected()
        {
            Trace.WriteLine("*******   ISecurityFacade reconected");
        }

        private void OnReconnecting()
        {
            Trace.WriteLine("******   ISecurityFacade reconecting");
        }

        public string Authenticate(string userName, string password)
        {
            CheckConnectionToServer();
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            _proxy.Invoke<string>("Authenticate", userName, password)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception != null) tcs.SetException(t.Exception);
                    }
                    else tcs.SetResult(t.Result);
                });

            return string.IsNullOrEmpty(task.Result) ? null : task.Result;
        }

        public List<User> GetUsers(string userToken)
        {
            CheckConnectionToServer();
            var tcs = new TaskCompletionSource<List<User>>();
            var task = tcs.Task;
            _proxy.Invoke<List<User>>("GetUsers",userToken)
                .ContinueWith(t => tcs.SetResult(t.Result));

            return task.Result;
        }

        public User GetUser(string userName)
        {
            CheckConnectionToServer();
            var tcs = new TaskCompletionSource<User>();
            var task = tcs.Task;
            _proxy.Invoke<User>("GetUser", userName)
                .ContinueWith(t => tcs.SetResult(t.Result));
            return task.Result;
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            _onCompleteAction = tcs.SetResult;

            CheckConnectionToServer();
            _proxy.Invoke("SaveNewUser", user, userRole, userToken);
            
            task.Wait();
            return task.Result;
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            _onCompleteAction =  tcs.SetResult;

            CheckConnectionToServer();
            _proxy.Invoke("UpdateUser", user, userRole, userToken);
           
            task.Wait();
        }

        public void RemoveUser(User user, string userToken)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = tcs.SetResult;

            CheckConnectionToServer();
            _proxy.Invoke("RemoveUser", user, userToken);
            
            task.Wait();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
