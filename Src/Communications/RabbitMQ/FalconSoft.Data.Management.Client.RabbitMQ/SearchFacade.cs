using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class SearchFacade : ISearchFacade
    {
        public SearchFacade(string serverUrl)
        {
            
        }

        public void Dispose()
        {
            
        }

        public SearchData[] Search(string searchString)
        {
            return null;
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            return null;
        }
    }
}