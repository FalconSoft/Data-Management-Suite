using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface IFacadesFactory
    {
        ICommandFacade CreateCommandFacade();

        IReactiveDataQueryFacade CreateReactiveDataQueryFacade();

        ITemporalDataQueryFacade CreateTemporalDataQueryFacade();

        IMetaDataAdminFacade CreateMetaDataAdminFacade();

        IMetaDataFacade CreateMetaDataFacade();

        ISearchFacade CreateSearchFacade();

        ISecurityFacade CreateSecurityFacade();
    }
}
