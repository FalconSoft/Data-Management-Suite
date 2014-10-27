using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
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

        [HttpPost]
        public HttpResponseMessage GetRecordsHistory([FromBody]DataSourceInfo dataSourceInfo, [FromUri]string recordKey)
        {
            _logger.Debug("Call TemporalDataApiController GetRecordsHistory");
            return EnumeratorToStream(_temporalDataQueryFacade.GetRecordsHistory(dataSourceInfo, recordKey), "GetRecordsHistory failed ");
        }

        [BindJson(typeof(TagInfo), "tagInfo")]
        [HttpPost]
        public HttpResponseMessage GetDataHistoryByTag([FromBody]DataSourceInfo dataSourceInfo, [FromUri]TagInfo tagInfo)
        {
            _logger.Debug("Call TemporalDataApiController GetDataHistoryByTag");
            return EnumeratorToStream(_temporalDataQueryFacade.GetDataHistoryByTag(dataSourceInfo, tagInfo), "GetDataHistoryByTag failed ");
        }

        [HttpPost]
        public HttpResponseMessage GetRecordsAsOf([FromBody]DataSourceInfo dataSourceInfo, [FromUri]DateTime timeStamp)
        {
            _logger.Debug("Call TemporalDataApiController GetRecordsAsOf");
           return EnumeratorToStream(_temporalDataQueryFacade.GetRecordsAsOf(dataSourceInfo, timeStamp), "GetRecordsAsOf failed ");
        }

       [HttpPost]
        public HttpResponseMessage GetTemporalDataByRevisionId([FromBody]DataSourceInfo dataSourceInfo,
            [FromUri]object revisionId)
        {
            _logger.Debug("Call TemporalDataApiController GetTemporalDataByRevisionId");
            return EnumeratorToStream(_temporalDataQueryFacade.GetTemporalDataByRevisionId(dataSourceInfo, revisionId), "GetTemporalDataByRevisionId failed ");
        }
        
        [HttpPost]
        public HttpResponseMessage GetRevisions([FromBody]DataSourceInfo dataSourceInfo)
        {
            _logger.Debug("Call TemporalDataApiController GetRevisions");
            return EnumeratorToStream(_temporalDataQueryFacade.GetRevisions(dataSourceInfo), "GetRevisions failed ");
        }

        [HttpGet]
        public HttpResponseMessage GeTagInfos()
        {
            _logger.Debug("Call TemporalDataApiController GeTagInfos");
            var responce = new HttpResponseMessage();

            try
            {
                var content = _temporalDataQueryFacade.GeTagInfos().ToArray();
                responce.StatusCode = HttpStatusCode.OK;
                responce.Content = new ObjectContent(typeof(TagInfo[]), content, new JsonMediaTypeFormatter());
                return responce;
            }
            
            catch (Exception ex)
            {
                _logger.Error("GeTagInfos failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage SaveTagInfo([FromBody]TagInfo tagInfo)
        {
            _logger.Debug("Call TemporalDataApiController SaveTagInfo");
            var responce = new HttpResponseMessage();
            try
            {
                _temporalDataQueryFacade.SaveTagInfo(tagInfo);
                responce.StatusCode = HttpStatusCode.OK;
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("SaveTagInfo failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage RemoveTagInfo([FromBody]TagInfo tagInfo)
        {
            _logger.Debug("Call TemporalDataApiController RemoveTagInfo");
            try
            {
                _temporalDataQueryFacade.RemoveTagInfo(tagInfo);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.Error("RemoveTagInfo failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
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