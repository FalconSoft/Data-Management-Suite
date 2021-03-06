﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.Data.Management.Server.WebAPI.Hubs
{
    public class MetaDataHub : Hub
    {

        public override Task OnConnected()
        {
            FacadesFactory.Logger.InfoFormat("[MetaDataHub] Client connected : {0} - {1}", Context.ConnectionId, Context.User);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            FacadesFactory.Logger.InfoFormat("[MetaDataHub] Client disconnected: {0} - {1}", Context.ConnectionId, Context.User);
            return base.OnDisconnected(stopCalled);
        }

    }
}
