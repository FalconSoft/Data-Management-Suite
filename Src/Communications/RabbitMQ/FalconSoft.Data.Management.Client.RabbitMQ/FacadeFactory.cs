using System;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class RabbitMqFacadesFactory : IFacadesFactory
    {
        private readonly string _serverUrl;
        private readonly string _userName;
        private readonly string _password;

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

            return new CommandFacade(_serverUrl, _userName, _password);
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new ReactiveDataQueryFacade(_serverUrl, _userName, _password);
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new TemporalDataQueryFacade(_serverUrl, _userName, _password);
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl, _userName, _password);
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl, _userName, _password);
        }

        public ISearchFacade CreateSearchFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SearchFacade(_serverUrl, _userName, _password);
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new PermissionSecurityFacade(_serverUrl, _userName, _password);
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SecurityFacade(_serverUrl, _userName, _password);
        }
    }
}
