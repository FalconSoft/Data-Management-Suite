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
        private IMetaDataAdminFacade _metaDataFacade;
        private ISearchFacade _searchFacade;
        private IPermissionSecurityFacade _sermissionSecurityFacade;
        private ISecurityFacade _securityFacade;

        public RabbitMqFacadesFactory(string serverUrl, string userName, string password)
        {
            _serverUrl = serverUrl;
            _userName = userName;
            _password = password;
        }

        public ICommandFacade CreateCommandFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _commandFacade ?? (_commandFacade = new CommandFacade(_serverUrl, _userName, _password));
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

            return _sermissionSecurityFacade ?? (_sermissionSecurityFacade = new PermissionSecurityFacade(_serverUrl, _userName, _password));
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return _securityFacade ?? (_securityFacade = new SecurityFacade(_serverUrl, _userName, _password));
        }
    }
}
