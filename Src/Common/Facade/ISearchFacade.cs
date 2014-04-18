using System;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface ISearchFacade : IDisposable
    {
        SearchData[] Search(string searchString);

        HeaderInfo[] GetSearchableWorksheets(SearchData searchData);
    }
}
