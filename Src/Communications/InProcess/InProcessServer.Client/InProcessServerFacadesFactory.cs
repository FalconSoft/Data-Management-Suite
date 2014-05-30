﻿using System;
using System.Configuration;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Server;

namespace FalconSoft.Data.Management.InProcessServer.Client
{
    public class InProcessServerFacadesFactory : IFacadesFactory
    {
        public InProcessServerFacadesFactory()
        {
            string metaDataPersistenceConnectionString;
            string persistenceDataConnectionString;
            string mongoDataConnectionString;
            ServerApp.Logger.InfoFormat("Server...");
            try
            {
                metaDataPersistenceConnectionString = ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"];
                persistenceDataConnectionString = ConfigurationManager.AppSettings["PersistenceDataConnectionString"];
                mongoDataConnectionString = ConfigurationManager.AppSettings["MongoDataConnectionString"];
            }
            catch (ConfigurationErrorsException ex)
            {
                ServerApp.Logger.ErrorFormat("Failed to Load DB Path. Error - {0}", ex.BareMessage);
                throw;
            }
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
