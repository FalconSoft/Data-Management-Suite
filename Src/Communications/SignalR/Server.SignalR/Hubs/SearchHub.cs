using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("ISearchFacade")]
    public class SearchHub : Hub
    {
        private readonly ISearchFacade _searchFacade;
        public SearchHub(ISearchFacade searchFacade)
        {
            _searchFacade = searchFacade;
        }

        public SearchData[] Search(string searchString)
        {
            return  _searchFacade.Search(searchString);
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            return  _searchFacade.GetSearchableWorksheets(searchData);
        }

    }
}
