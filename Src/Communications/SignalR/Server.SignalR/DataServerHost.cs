using System;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using Microsoft.Owin.Hosting;

namespace FalconSoft.Data.Management.Server.SignalR
{
    public class DataServerHost : IDataServer
    {
        IDisposable SignalR { get; set; }

        public DataServerHost(string connectionString, ILogger logger, ICommandFacade commandFacade, IMetaDataAdminFacade metaDataAdminFacade,
                IReactiveDataQueryFacade reactiveDataQueryFacade, ITemporalDataQueryFacade temporalDataQueryFacade,
                ISearchFacade searchFacade, ISecurityFacade securityFacade)
        {
            HubServer.ConnectionString = connectionString;
            HubServer.Logger = logger;
            HubServer.CommandFacade = commandFacade;
            HubServer.MetaDataAdminFacade = metaDataAdminFacade;
            HubServer.ReactiveDataQueryFacade = reactiveDataQueryFacade;
            HubServer.TemporalDataQueryFacade = temporalDataQueryFacade;
            HubServer.SearchFacade = searchFacade;
            HubServer.SecurityFacade = securityFacade;
        }

        public void StartServer()
        {
            SignalR = WebApp.Start<HubServer>(HubServer.ConnectionString);
            HubServer.Logger.InfoFormat("Server is running, on address {0} Press <Enter> to stop", HubServer.ConnectionString);
        }

        public void StopServer()
        {
            HubServer.Logger.Info("Server stopped running...");
            if (SignalR!=null)
                SignalR.Dispose();
        }
    }
}
