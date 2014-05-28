using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FalconSoft.Data.Server.Common;
using FalconSoft.Data.Server.Common.Facade;
using Microsoft.Owin.Hosting;

namespace FalconSoft.Data.Management.Server.SignalR
{

    public class DataServerHost : IDataServer
    {
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


        public void Start()
        {
            using (WebApp.Start<HubServer>(HubServer.ConnectionString))
            {
                HubServer.Logger.InfoFormat("Server is running, on address {0} Press <Enter> to stop", HubServer.ConnectionString);
                Console.ReadLine();
            }              
        }
    }
}
