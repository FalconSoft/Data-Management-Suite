using System;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        private string url = "http://localhost:8080";
        private RabbitMQClient _rabbitMQClient;
        private ICommandFacade _commandFacade;
        private IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private ITemporalDataQueryFacade _temporalDataQueryFacade;
        private IMetaDataAdminFacade _metaDataAdmnFacade;
        private ISearchFacade _searchFacade;
        private ISecurityFacade _securityFacade;
        private IPermissionSecurityFacade _permissionSecurityFacade;

        public WebApiFacadeFactory(string serverUrl, string userName, string password)
        {
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
            return _commandFacade ?? (_commandFacade = new CommandFacade(url));
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return _reactiveDataQueryFacade ?? (_reactiveDataQueryFacade = new ReactiveDataQueryFacade("http://localhost:8080", _rabbitMQClient));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return _temporalDataQueryFacade ?? (_temporalDataQueryFacade =  new TemporalDataQueryFacade(url));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return _metaDataAdmnFacade ??  (_metaDataAdmnFacade = new MetaDataAdminFacade("http://localhost:8080", _rabbitMQClient));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade("http://localhost:8080", _rabbitMQClient));
        }

        public ISearchFacade CreateSearchFacade()
        {
            return _searchFacade ??  (_searchFacade = new SearchFacade(url));
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return _securityFacade ?? (_securityFacade = new SecurityFacade("http://localhost:8080", _rabbitMQClient));
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return _permissionSecurityFacade ?? (_permissionSecurityFacade = new PermissionSecurityFacade("http://localhost:8080", _rabbitMQClient));
        }

        public ITestFacade CreateTestFacade()
        {
            return new TestFacade();
        }

        public void Dispose()
        {
            //_rabbitMQClient.Close();
        }
    }
}
