using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class SearchFacade : RabbitMQFacadeBase, ISearchFacade
    {
        private const string SearchFacadeQueueName = "SearchFacadeRPC";

        public SearchFacade(string serverUrl, string userName, string password):base(serverUrl, userName, password)
        {
            InitializeConnection(SearchFacadeQueueName);
            KeepAliveAction = () => InitializeConnection(SearchFacadeQueueName);
        }

        public SearchData[] Search(string searchString)
        {
            return RPCServerTaskExecute<SearchData[]>(Connection, SearchFacadeQueueName, "Search", null,
                new object[] { searchString });
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            return RPCServerTaskExecute<HeaderInfo[]>(Connection, SearchFacadeQueueName, "GetSearchableWorksheets",
                null, new object[] { searchData });
        }

        public void Dispose()
        {

        }

        public new void Close()
        {
            base.Close();
        }

    }
}