using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.Data.Management.Server.WebAPI.Hubs
{
    public class ReactiveDataHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.SomeTestMessage(name, message);
        }
        public override Task OnConnected()
        {
            Console.WriteLine("Client connected: " + Context.ConnectionId);

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
                
            Console.WriteLine("Client disconnected: " + Context.ConnectionId);

            return base.OnDisconnected(stopCalled);
        }

    }
}
