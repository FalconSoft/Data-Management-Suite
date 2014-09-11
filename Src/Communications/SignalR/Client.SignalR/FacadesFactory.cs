using System;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.SignalR
{
    public class SignalRFacadesFactory : IFacadesFactory
    {
        private readonly string _serverUrl;

        private CommandFacade _commandFacade;
        private ReactiveDataQueryFacade _reactiveDataQueryFacade;
        private TemporalDataQueryFacade _temporalDataQueryFacade;
        private MetaDataFacade _metaDataFacade;
        private SearchFacade _searchFacade;
        private PermissionSecurityFacade _permissionSecurityFacade;
        private SecurityFacade _securityFacade;

        public SignalRFacadesFactory(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public ICommandFacade CreateCommandFacade()
        {
            if(string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _commandFacade ?? (_commandFacade = new CommandFacade(_serverUrl));
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");
            
            return _reactiveDataQueryFacade ??
                   (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(_serverUrl));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _temporalDataQueryFacade ??
                   (_temporalDataQueryFacade = new TemporalDataQueryFacade(_serverUrl));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(_serverUrl));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(_serverUrl));
        }

        public ISearchFacade CreateSearchFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _searchFacade ?? (_searchFacade = new SearchFacade(_serverUrl));
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _permissionSecurityFacade ?? (_permissionSecurityFacade = new PermissionSecurityFacade(_serverUrl));
        }

        public ITestFacade CreateTestFacade()
        {
            return null;
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _securityFacade ?? (_securityFacade = new SecurityFacade(_serverUrl));
        }

        public void Dispose()
        {
            if (_commandFacade != null) _commandFacade.Dispose();

            if (_reactiveDataQueryFacade != null) _reactiveDataQueryFacade.Dispose();

            if (_temporalDataQueryFacade != null) _temporalDataQueryFacade.Dispose();

            if (_metaDataFacade != null) _metaDataFacade.Dispose();

            if (_searchFacade != null) _searchFacade.Dispose();

            if (_permissionSecurityFacade != null) _permissionSecurityFacade.Dispose();

            if (_securityFacade != null) _securityFacade.Dispose();
        }
    }
}
