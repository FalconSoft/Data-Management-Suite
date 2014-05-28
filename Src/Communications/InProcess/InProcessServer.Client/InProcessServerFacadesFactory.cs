using System;
using FalconSoft.Data.Server;
using FalconSoft.Data.Server.Common.Facade;

namespace FalconSoft.ReactiveWorksheets.InProcessServer.Client
{
    public class InProcessServerFacadesFactory : IFacadesFactory
    {
        public InProcessServerFacadesFactory(string metaDataPersistenceConnectionString, string persistenceDataConnectionString, string mongoDataConnectionString)
        {
            ServerApp.Logger.InfoFormat("Server...");
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(metaDataPersistenceConnectionString, persistenceDataConnectionString, mongoDataConnectionString);
                bootstrapper.Run();
            }
            catch (Exception ex)
            {
                ServerApp.Logger.Error("Failed to Configure and Run Bootstrapper", ex);
                throw;
            }
        }

        public ICommandFacade CreateCommandFacade()
        {
            return ServerApp.CommandFacade;
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return ServerApp.ReactiveDataQueryFacade;
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return ServerApp.TemporalQueryFacade;
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return ServerApp.MetaDataFacade;
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return ServerApp.MetaDataFacade;
        }

        public ISearchFacade CreateSearchFacade()
        {
            return ServerApp.SearchFacade;
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return ServerApp.SecurityFacade;
        }
    }
}
