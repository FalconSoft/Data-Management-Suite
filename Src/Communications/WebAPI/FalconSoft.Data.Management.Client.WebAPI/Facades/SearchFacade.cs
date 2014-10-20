using System;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SearchFacade : ISearchFacade
    {
        public SearchData[] Search(string searchString)
        {
            throw new NotImplementedException();
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }
    }
}