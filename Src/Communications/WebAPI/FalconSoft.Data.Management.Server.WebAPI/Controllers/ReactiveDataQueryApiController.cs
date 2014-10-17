using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;
using Newtonsoft.Json;

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

        [BindJson(typeof(AggregatedWorksheetInfo), "aggregatedWorksheet")]
        [BindJson(typeof(FilterRule[]), "filterRules")]
        public HttpResponseMessage GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            return EnumeratorToStream(_reactiveDataQueryFacade.GetAggregatedData(userToken, dataSourcePath,
                    aggregatedWorksheet, filterRules), "GetAggregatedData failed ");
        }

        [BindJson(typeof(string[]), "fields")]
        [BindJson(typeof(FilterRule[]), "filterRules")]
        public HttpResponseMessage GetData(string userToken, string dataSourcePath, [FromUri]string[] fields, [FromUri]FilterRule[] filterRules)
        {
            return EnumeratorToStream(_reactiveDataQueryFacade.GetData(userToken, dataSourcePath, fields, filterRules), "GetData failed ");
        }

        public HttpResponseMessage GetFieldData(string userToken, string dataSourcePath, string field, string match, int elementsToReturn = 10)
        {
            return EnumeratorToStream(_reactiveDataQueryFacade.GetFieldData(userToken, dataSourcePath, field, match, elementsToReturn), "GetFieldData failed ");
            //try
            //{
            //    return _reactiveDataQueryFacade.GetFieldData(userToken, dataSourcePath, field, match, elementsToReturn);
            //}
            //catch (Exception ex)
            //{
            //    _logger.Error("GetFieldData failed ", ex);
            //    return Enumerable.Empty<string>();
            //}
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

        private HttpResponseMessage EnumeratorToStream<T>(IEnumerable<T> enumerable, string errorMessage)
        {
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = new PushStreamContent((outputStream, httpContent, transportContext) => new Task(() =>
            {
                try
                {
                    // Execute the command and get a reader
                    var sw = new BinaryWriter(outputStream);
                    var jsonConverter = new JavaScriptSerializer();
                    foreach (var dictionary in enumerable)
                    {
                        var json = jsonConverter.Serialize(dictionary) + "[#]";

                        var buffer = Encoding.UTF8.GetBytes(json);

                        // Write out data to output stream
                        sw.Write(buffer);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(errorMessage, ex);
                    throw;
                }
                finally
                {
                    outputStream.Close();
                }

            }).Start());

            return response;
        }
    }
}
