using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("ISearchFacade")]
    public class SearchHub : Hub
    {
        private readonly ISearchFacade _searchFacade;
        public SearchHub(ISearchFacade searchFacade)
        {
            _searchFacade = searchFacade;
        }

        public async Task<SearchData[]> Search(string searchString)
        {
            return await Task.Run(() => _searchFacade.Search(searchString));
        }

        public async Task<HeaderInfo[]> GetSearchableWorksheets(SearchData searchData)
        {
            return await Task.Run(() => _searchFacade.GetSearchableWorksheets(searchData));
        }

    }
}
