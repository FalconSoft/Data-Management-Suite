using System;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.SignalR
{
    public class SignalRFacadesFactory : IFacadesFactory
    {
        private readonly string _serverUrl;

        public SignalRFacadesFactory(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public ICommandFacade CreateCommandFacade()
        {
            if(string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new CommandFacade(_serverUrl);
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");
            
            return new ReactiveDataQueryFacade(_serverUrl);
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new TemporalDataQueryFacade(_serverUrl);
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl);
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl);
        }

        public ISearchFacade CreateSearchFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SearchFacade(_serverUrl);
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SecurityFacade(_serverUrl);
        }
    }
}
