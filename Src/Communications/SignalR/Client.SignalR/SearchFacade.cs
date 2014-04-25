using System.Diagnostics;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class SearchFacade : ISearchFacade
    {
        private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private readonly Task _startConnectionTask;

        public SearchFacade(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("ISearchFacade");

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
            Trace.WriteLine("*******   ISearchFacade reconected");
            //throw new NotImplementedException();
        }

        private void OnReconnecting()
        {
            Trace.WriteLine("******   ISearchFacade reconecting");
            //throw new NotImplementedException();
        }

        public SearchData[] Search(string searchString)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return SearchServerCall(searchString);
            }
            _startConnectionTask.Wait();
            return SearchServerCall(searchString);
        }

        private SearchData[] SearchServerCall(string searchString)
        {
            var tcs = new TaskCompletionSource<SearchData[]>();
            var task = tcs.Task;
            _proxy.Invoke<SearchData[]>("Search", searchString)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    var data = t.Result;
                    tcs.SetResult(data);
                });
            return task.Result;
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetSearchableWorksheetsServerCall(searchData);
            }
            _startConnectionTask.Wait();
            return GetSearchableWorksheetsServerCall(searchData);
        }

        private HeaderInfo[] GetSearchableWorksheetsServerCall(SearchData searchData)
        {
            var tcs = new TaskCompletionSource<HeaderInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<HeaderInfo[]>("GetSearchableWorksheets", searchData)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    var data = t.Result;
                    tcs.SetResult(data);
                });
            return task.Result;
        }

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
