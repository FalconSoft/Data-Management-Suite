using System;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class RabbitMqFacadesFactory : IFacadesFactory
    {
        private readonly string _serverUrl;
        private readonly string _userName;
        private readonly string _password;
        private CommandFacade _commandFacade;
        private ReactiveDataQueryFacade _reactiveDataQueryFacade;
        private TemporalDataQueryFacade _temporalDataQueryFacade;
        private MetaDataFacade _metaDataFacade;
        private SearchFacade _searchFacade;
        private PermissionSecurityFacade _permissionSecurityFacade;
        private SecurityFacade _securityFacade;

        public RabbitMqFacadesFactory(string serverUrl, string userName, string password)
        {
            _serverUrl = serverUrl;
            _userName = userName;
            _password = password;

            _commandFacade = new CommandFacade(_serverUrl, _userName, _password);
        }

        public ICommandFacade CreateCommandFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _commandFacade;
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _reactiveDataQueryFacade ??
                   (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(_serverUrl, _userName, _password));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _temporalDataQueryFacade ??
                   (_temporalDataQueryFacade = new TemporalDataQueryFacade(_serverUrl, _userName, _password));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(_serverUrl, _userName, _password));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(_serverUrl, _userName, _password));
        }

        public ISearchFacade CreateSearchFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _searchFacade ?? (_searchFacade = new SearchFacade(_serverUrl, _userName, _password));
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _permissionSecurityFacade ?? (_permissionSecurityFacade = new PermissionSecurityFacade(_serverUrl, _userName, _password));
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _securityFacade ?? (_securityFacade = new SecurityFacade(_serverUrl, _userName, _password));
        }

        public void Dispose()
        {
            _commandFacade.Close();
            _reactiveDataQueryFacade.Close();
            _temporalDataQueryFacade.Close();
            _metaDataFacade.Close();
            _searchFacade.Close();
            _permissionSecurityFacade.Close();
            _securityFacade.Close();
        }
    }
}
