using System;
using System.Collections.Generic;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class TemporalDataApiController : ApiController
    {
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        private readonly ILogger _logger;

        public TemporalDataApiController(ITemporalDataQueryFacade temporalDataQueryFacade, ILogger logger)
        {
            _temporalDataQueryFacade = temporalDataQueryFacade;
            _logger = logger;
        }

        [BindJson(typeof(DataSourceInfo), "dataSourceInfo")]
        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            try
            {
                return _temporalDataQueryFacade.GetRecordsHistory(dataSourceInfo, recordKey);
            }
            catch (Exception ex)
            {
                _logger.Error("GetRecordsHistory failed ", ex);
                return new List<Dictionary<string, object>>();
            }
        }

        [BindJson(typeof(DataSourceInfo), "dataSourceInfo")]
        [BindJson(typeof(TagInfo), "tagInfo")]
        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            try
            {
                return _temporalDataQueryFacade.GetDataHistoryByTag(dataSourceInfo, tagInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("GetDataHistoryByTag failed ", ex);
                return new List<Dictionary<string, object>>(); ;
            }
        }

        [BindJson(typeof(DataSourceInfo), "dataSourceInfo")]
        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            try
            {
                return _temporalDataQueryFacade.GetRecordsAsOf(dataSourceInfo, timeStamp);
            }
            catch (Exception ex)
            {
                _logger.Error("GetRecordsAsOf failed ", ex);
                return new List<Dictionary<string, object>>();
            }
        }

        [BindJson(typeof(DataSourceInfo), "dataSourceInfo")]
        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo,
            object revisionId)
        {
            try
            {
                return _temporalDataQueryFacade.GetTemporalDataByRevisionId(dataSourceInfo, revisionId);
            }
            catch (Exception ex)
            {
                _logger.Error("GetTemporalDataByRevisionId failed ", ex);
                return new List<Dictionary<string, object>>();
            }
        }
        
        [BindJson(typeof(DataSourceInfo), "dataSourceInfo")]
        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            try
            {
                return _temporalDataQueryFacade.GetRevisions(dataSourceInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("GetRevisions failed ", ex);
                return new List<Dictionary<string, object>>();
            }
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            try
            {
                return _temporalDataQueryFacade.GeTagInfos();
            }
            catch (Exception ex)
            {
                _logger.Error("GeTagInfos failed ", ex);
                return new List<TagInfo>();
            }
        }

        [BindJson(typeof(TagInfo), "tagInfo")]
        public void SaveTagInfo(TagInfo tagInfo)
        {
            try
            {
                _temporalDataQueryFacade.SaveTagInfo(tagInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("SaveTagInfo failed ", ex);
            }
        }

        [BindJson(typeof(TagInfo), "tagInfo")]
        public void RemoveTagInfo(TagInfo tagInfo)
        {
            try
            {
                _temporalDataQueryFacade.RemoveTagInfo(tagInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("RemoveTagInfo failed ", ex);
            }
        }
    }
}