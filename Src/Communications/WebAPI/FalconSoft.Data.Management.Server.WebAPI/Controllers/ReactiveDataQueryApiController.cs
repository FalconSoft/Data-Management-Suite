using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public sealed class ReactiveDataQueryApiController : ApiController
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ILogger _logger;

        public ReactiveDataQueryApiController(IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;
        }

        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            try
            {
                return _reactiveDataQueryFacade.GetAggregatedData(userToken, dataSourcePath, aggregatedWorksheet,
                    filterRules);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAggregatedData failed ", ex);
                return Enumerable.Empty<Dictionary<string, object>>();
            }
        }

        public IEnumerable<T> GetData<T>(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            try
            {
                return _reactiveDataQueryFacade.GetData<T>(userToken, dataSourcePath, filterRules);
            }
            catch (Exception ex)
            {
                _logger.Error("GetData<T> failed ", ex);
                return Enumerable.Empty<T>();
            }
        }

        public IEnumerable<Dictionary<string, object>> GetData(string userToken, string dataSourcePath, string[] fields = null, FilterRule[] filterRules = null)
        {
            try
            {
                return _reactiveDataQueryFacade.GetData(userToken, dataSourcePath, fields, filterRules);
            }
            catch (Exception ex)
            {
                _logger.Error("GetData failed ", ex);
                return Enumerable.Empty<Dictionary<string, object>>();
            }
        }

        public IEnumerable<string> GetFieldData(string userToken, string dataSourcePath, string field, string match, int elementsToReturn = 10)
        {
            try
            {
                return _reactiveDataQueryFacade.GetFieldData(userToken, dataSourcePath, field, match, elementsToReturn);
            }
            catch (Exception ex)
            {
                _logger.Error("GetFieldData failed ", ex);
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<Dictionary<string, object>> GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys, string[] fields = null)
        {
            try
            {
                return _reactiveDataQueryFacade.GetDataByKey(userToken, dataSourcePath, recordKeys, fields);
            }
            catch (Exception ex)
            {
                _logger.Error("GetDataByKey failed ", ex);
                return Enumerable.Empty<Dictionary<string, object>>();
            }
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            return null;
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            try
            {
                _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecord, dataSourceUrn, userToken, onSuccess, onFail);
            }
            catch (Exception ex)
            {
                _logger.Error("ResolveRecordbyForeignKey failed ", ex);
            }
        }

        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            try
            {
                return _reactiveDataQueryFacade.CheckExistence(userToken, dataSourceUrn, fieldName, value);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAggregatedData failed ", ex);
                return false;
            }
        }
    }
}
