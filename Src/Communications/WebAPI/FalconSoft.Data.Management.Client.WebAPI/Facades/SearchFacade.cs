using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SearchFacade : WebApiClientBase, ISearchFacade
    {
        public SearchFacade(string url, IRabbitMQClient client)
            : base(url, "SearchApi", client)
        {
            
        }
        public SearchData[] Search(string searchString)
        {
            return GetWebApiCall<SearchData[]>("Search", new Dictionary<string, object>
            {
                {"searchString", searchString}
            });
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            return GetWebApiCall<HeaderInfo[]>("GetSearchableWorksheets", new Dictionary<string, object>
            {
                {"searchData", searchData}
            });
        }

        public void Dispose()
        {

        }
    }
}