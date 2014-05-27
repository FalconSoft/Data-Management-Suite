using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FalconSoft.Data.Server.Common.Facade;
using FalconSoft.Data.Server.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class SecurityFacade : ISecurityFacade
    {
        private readonly string _connectionString;
        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;
        private Action _onCompleteAction;

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

            _proxy.On("OnComplete", () =>
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

        public List<User> GetUsers()
        {
            CheckConnectionToServer();
            var tcs = new TaskCompletionSource<List<User>>();
            var task = tcs.Task;
            _proxy.Invoke<List<User>>("GetUsers")
                .ContinueWith(t => tcs.SetResult(t.Result));

            return task.Result;
        }

        public void SaveNewUser(User user)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => tcs.SetResult(new object());

            CheckConnectionToServer();
            _proxy.Invoke("SaveNewUser", user);
            
            task.Wait();
        }

        public void UpdateUser(User user)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => tcs.SetResult(new object());

            CheckConnectionToServer();
            _proxy.Invoke("UpdateUser", user);
           
            task.Wait();
        }

        public void RemoveUser(User user)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => tcs.SetResult(new object());
            CheckConnectionToServer();
            _proxy.Invoke("RemoveUser", user);
            
            task.Wait();
        }

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
