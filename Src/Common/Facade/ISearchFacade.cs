using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface ISearchFacade
    {
        SearchData[] Search(string searchString);

        HeaderInfo[] GetSearchableWorksheets(SearchData searchData);
    }
}
