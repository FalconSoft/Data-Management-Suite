using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using Microsoft.AspNet.SignalR.Client;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    internal class SignalRHub : IDisposable
    {
        private readonly string _pushUrl;
        private readonly string _hubName;
        private readonly ILogger _log;
        private HubConnection _connection = null;
        private Action<IHubProxy> _hubProxyAction;

        public SignalRHub(string pushUrl, string hubName, ILogger log, Action<IHubProxy> hubProxyAction)
        {
            _pushUrl = pushUrl;
            _hubName = hubName;
            _log = log;
            _hubProxyAction = hubProxyAction;
            ConnectAsync(_pushUrl, _hubName, _hubProxyAction);
        }


        private async void ConnectAsync(string url, string hubName, Action<IHubProxy> hubProxyAction)
        {
            _connection = new HubConnection(url);
            _connection.Closed += Connection_Closed;
            var hubProxy = _connection.CreateHubProxy(hubName);

            hubProxyAction(hubProxy);

            try
            {
                await _connection.Start();
            }
            catch (HttpRequestException ex)
            {
                _log.Error("+ Unable to connect to server: Start server before connecting clients.", ex);
                return;
            }

            _log.InfoFormat("+ Connected to {0} server, ConnectionId= {1} ", hubName, _connection.ConnectionId);
        }

        private void Connection_Closed()
        {
            _connection.Closed -= Connection_Closed;
            _log.InfoFormat("+ Connection with {0} closed", _hubName);
            //            if (!_disposing)
            {
                _log.InfoFormat("+ Reconnecting to {0}...", _hubName);
                ConnectAsync(_pushUrl, _hubName, _hubProxyAction);
            }
        }



        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Stop();
                _connection.Dispose();
            }
        }
    }
}
