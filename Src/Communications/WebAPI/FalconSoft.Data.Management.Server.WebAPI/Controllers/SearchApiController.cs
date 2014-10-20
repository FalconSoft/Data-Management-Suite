using System;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;

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

        [HttpGet]
        public SearchData[] Search([FromUri]string searchString)
        {
            _logger.Debug("Call SearchApiController Search");
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

        [BindJson(typeof(SearchData), "searchData")]
        [HttpGet]
        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            _logger.Debug("Call SearchApiController GetSearchableWorksheets");
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
