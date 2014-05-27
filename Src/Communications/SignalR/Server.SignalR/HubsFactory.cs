using System;
using FalconSoft.Data.Server.Common.Facade;
using Microsoft.AspNet.SignalR;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR
{
    public static class HubsFactory
    {
        public static Hub CreateReactiveDataQueryHub(IReactiveDataQueryFacade queryFacade)
        {
            throw new NotImplementedException();
        }

        public static Hub CreateMetaDataHub(IMetaDataAdminFacade metaDataFacade)
        {
            throw new NotImplementedException();
        }

        public static Hub CreateTemporalDataQueryFacade(ITemporalDataQueryFacade temporalDataFacade)
        {
            throw new NotImplementedException();
        }

        public static Hub CreateCommandsFacade(ICommandFacade temporalDataFacade)
        {
            throw new NotImplementedException();
        }

        public static Hub CreateSearchFacade(ISearchFacade searchFacade)
        {
            throw new NotImplementedException();
        }
    }
}
