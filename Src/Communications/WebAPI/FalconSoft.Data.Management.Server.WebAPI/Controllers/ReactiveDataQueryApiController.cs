using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
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
        [HttpGet]
        public HttpResponseMessage GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetAggregatedData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetAggregatedData(userToken, dataSourcePath,
                    aggregatedWorksheet, filterRules), "GetAggregatedData failed ");
        }

        [BindJson(typeof(string[]), "fields")]
        [BindJson(typeof(FilterRule[]), "filterRules")]
        [HttpGet]
        public HttpResponseMessage GetData(string userToken, string dataSourcePath, [FromUri]string[] fields, [FromUri]FilterRule[] filterRules)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetData(userToken, dataSourcePath, fields, filterRules), "GetData failed ");
        }

        [HttpGet]
        public HttpResponseMessage GetFieldData(string userToken, string dataSourcePath, string field, string match, int elementsToReturn = 10)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetFieldData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetFieldData(userToken, dataSourcePath, field, match, elementsToReturn), "GetFieldData failed ");
        }

        [BindJson(typeof(string[]), "recordKeys")]
        [BindJson(typeof(string[]), "fields")]
        [HttpGet]
        public HttpResponseMessage GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys, string[] fields = null)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetDataByKey");
           
            return EnumeratorToStream(_reactiveDataQueryFacade.GetDataByKey(userToken, dataSourcePath, recordKeys, fields), "GetDataByKey failed ");
        }

        [BindJson(typeof(RecordChangedParam[]), "changedRecord")]
        [HttpGet]
        public HttpResponseMessage ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken)
        {
            _logger.Debug("Call ReactiveDataQueryApiController ResolveRecordbyForeignKey");
            
            try
            {
                var responce = new HttpResponseMessage();
                _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecord, dataSourceUrn, userToken,
                    (msg, rpcArray) =>
                    {
                        responce.ReasonPhrase = msg;
                        responce.StatusCode = HttpStatusCode.OK;
                        responce.Content = new ObjectContent(typeof(RecordChangedParam[]),rpcArray, new JsonMediaTypeFormatter());
                    }, (msg, ex) =>
                    {
                        responce.ReasonPhrase = msg;
                        responce.StatusCode = HttpStatusCode.InternalServerError;
                        responce.Content = new ObjectContent(typeof(Exception), ex, new JsonMediaTypeFormatter());
                    });

                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("ResolveRecordbyForeignKey failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            _logger.Debug("Call ReactiveDataQueryApiController CheckExistence");
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

        // helper method to enumerate data into stream
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
