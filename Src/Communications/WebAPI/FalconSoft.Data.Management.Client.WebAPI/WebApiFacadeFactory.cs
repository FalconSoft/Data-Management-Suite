using System;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        public WebApiFacadeFactory()
        {
            
        }
        public ICommandFacade CreateCommandFacade()
        {
            return new CommandFacade();
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return new ReactiveDataQueryFacade("http://localhost:8080");
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return new TemporalDataQueryFacade();
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return new MetaDataAdminFacade("http://localhost:8080");
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return new MetaDataAdminFacade("http://localhost:8080");
        }

        public ISearchFacade CreateSearchFacade()
        {
            return new SearchFacade();
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return new SecurityFacade("http://localhost:8080");
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return new PermissionSecurityFacade("http://localhost:8080");
        }

        public ITestFacade CreateTestFacade()
        {
            return new TestFacade();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
