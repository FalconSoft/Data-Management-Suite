using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR
{
    internal class LoggingPipelineModule : HubPipelineModule
    {
        protected override void OnIncomingError(Exception ex, IHubIncomingInvokerContext context)
        {
            Console.WriteLine("=>" + DateTime.Now.ToString("HH:mm:ss") + " Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            base.OnIncomingError(ex, context);
        }

        protected override bool OnBeforeConnect(IHub hub)
        {
            Console.WriteLine("=>" + DateTime.Now.ToString("HH:mm:ss") + " OnBeforeConnect " + hub.Context.QueryString);
            return base.OnBeforeConnect(hub);
        }

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            Console.WriteLine("=>" + DateTime.Now.ToString("HH:mm:ss") + " Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }
        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            Console.WriteLine("<=" + DateTime.Now.ToString("HH:mm:ss") + " Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }
    }
}
