using System;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        private string url = "http://localhost:8080";
        private RabbitMQClient _rabbitMQClient;

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
            return new CommandFacade(url);
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return new ReactiveDataQueryFacade("http://localhost:8080", _rabbitMQClient);
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return new TemporalDataQueryFacade(url);
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return new MetaDataAdminFacade("http://localhost:8080", _rabbitMQClient);
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return new MetaDataAdminFacade("http://localhost:8080", _rabbitMQClient);
        }

        public ISearchFacade CreateSearchFacade()
        {
            return new SearchFacade(url);
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return new SecurityFacade("http://localhost:8080", _rabbitMQClient);
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return new PermissionSecurityFacade("http://localhost:8080", _rabbitMQClient);
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
