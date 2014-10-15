using System;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class SearchApiController : ApiController
    {
        private readonly ISearchFacade _searchFacade;
        private readonly ILogger _logger;

        public SearchApiController(ISearchFacade searchFacade, ILogger logger)
        {
            _searchFacade = searchFacade;
            _logger = logger;
        }

        public SearchData[] Search(string searchString)
        {
            try
            {
                return _searchFacade.Search(searchString);
            }
            catch (Exception ex)
            {
                _logger.Error("Search failed ", ex);
                return new SearchData[0];
            }
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            try
            {
                return _searchFacade.GetSearchableWorksheets(searchData);
            }
            catch (Exception ex)
            {
                _logger.Error("GetSearchableWorksheets failed ", ex);
                return new HeaderInfo[0];
            }
        }
    }
}
