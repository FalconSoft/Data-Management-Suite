﻿using System;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Server;

namespace FalconSoft.Data.Management.InProcessServer.Client
{
    public class InProcessServerFacadesFactory : IFacadesFactory
    {
        public InProcessServerFacadesFactory(string metaDataPersistenceConnectionString, string persistenceDataConnectionString, string mongoDataConnectionString, string connectionString, string dataSourcesPath)
        {
            ServerApp.Logger.InfoFormat("Server...");
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(metaDataPersistenceConnectionString, persistenceDataConnectionString, mongoDataConnectionString, connectionString, dataSourcesPath);
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

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return ServerApp.PermissionSecurityFacade;
        }

        public ITestFacade CreateTestFacade()
        {
            return null;
        }

        public void Dispose()
        {
            
        }

        public AuthenticationResult Authenticate(string url, string companyName, string userName, string password)
        {
            throw new NotImplementedException();
        }
    }
}
