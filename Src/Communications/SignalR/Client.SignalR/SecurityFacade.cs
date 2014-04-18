using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Security;
using Microsoft.AspNet.SignalR.Client;

namespace ReactiveWorksheets.Client.SignalR
{
    internal class SecurityFacade : ISecurityFacade
    {

         private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private readonly Task _startConnectionTask;

        public SecurityFacade(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("ISecurityFacade");

            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
            _connection.Closed += OnClosed;

            _startConnectionTask = _connection.Start();
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
            if (_startConnectionTask.IsCompleted)
            {
                var tcs = new TaskCompletionSource<List<User>>();
                var task = tcs.Task;
                _proxy.Invoke<List<User>>("GetUsers")
                    .ContinueWith(t => tcs.SetResult(t.Result));

                return task.Result;
            }
            else
            {
                var tcs = new TaskCompletionSource<List<User>>();
                var task = tcs.Task;
                _startConnectionTask.ContinueWith(t =>
                    _proxy.Invoke<List<User>>("GetUsers")
                        .ContinueWith(t1 => tcs.SetResult(t1.Result)));

                return task.Result;
            }
        }

        public void SaveNewUser(User user)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("SaveNewUser", user);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("SaveNewUser", user));
            }
        }

        public void UpdateUser(User user)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("UpdateUser", user);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("UpdateUser", user));
            }
        }

        public void RemoveUser(User user)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("RemoveUser", user);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("RemoveUser", user));
            }
        }

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
