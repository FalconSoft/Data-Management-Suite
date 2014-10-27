using System;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        private readonly string _url ;
        private RabbitMQClient _rabbitMQClient;
        private ICommandFacade _commandFacade;
        private IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private ITemporalDataQueryFacade _temporalDataQueryFacade;
        private IMetaDataAdminFacade _metaDataAdmnFacade;
        private ISearchFacade _searchFacade;
        private ISecurityFacade _securityFacade;
        private IPermissionSecurityFacade _permissionSecurityFacade;

        public WebApiFacadeFactory(string url, string serverUrl, string userName, string password)
        {
            _url = url;
            try
            {
                _rabbitMQClient = new RabbitMQClient(serverUrl, userName, password, "/");
            }
            catch (Exception)
            {
                _rabbitMQClient = null;
            }
        }

        public ICommandFacade CreateCommandFacade()
        {
            return _commandFacade ?? (_commandFacade = new CommandFacade(_url, _rabbitMQClient));
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return _reactiveDataQueryFacade ?? (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(_url, _rabbitMQClient));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return _temporalDataQueryFacade ?? (_temporalDataQueryFacade = new TemporalDataQueryFacade(_url, _rabbitMQClient));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _rabbitMQClient));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _rabbitMQClient));
        }

        public ISearchFacade CreateSearchFacade()
        {
            return _searchFacade ?? (_searchFacade = new SearchFacade(_url, _rabbitMQClient));
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return _securityFacade ?? (_securityFacade = new SecurityFacade(_url, _rabbitMQClient));
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return _permissionSecurityFacade ?? (_permissionSecurityFacade = new PermissionSecurityFacade(_url, _rabbitMQClient));
        }

        public ITestFacade CreateTestFacade()
        {
            return new TestFacade();
        }

        public void Dispose()
        {
            if (_rabbitMQClient!=null)
                _rabbitMQClient.Close();
        }
    }
}
