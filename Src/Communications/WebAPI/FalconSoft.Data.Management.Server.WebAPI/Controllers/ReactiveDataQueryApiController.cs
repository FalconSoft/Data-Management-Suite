using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;
using Newtonsoft.Json;
using Microsoft.AspNet.SignalR;
using FalconSoft.Data.Management.Server.WebAPI.Hubs;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public sealed class ReactiveDataQueryApiController : ApiController
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ILogger _logger;
        private readonly Lazy<IHubContext> _reactiveDataHub = new Lazy<IHubContext>(() => GlobalHost.ConnectionManager.GetHubContext<ReactiveDataHub>());
        private volatile static Dictionary<string, string> _pushMessages = null;
        private static object _locker = new object();
        private static bool _subscribed = false;

        public Dictionary<string, string> PushMessages
        {
            get
            {
                if (_pushMessages == null)
                {
                    lock (_locker)
                    {
                        if (_pushMessages == null)
                            _pushMessages = new Dictionary<string, string>();
                    }
                }
                return _pushMessages;
            }
        }

        public ReactiveDataQueryApiController()
        {
            _reactiveDataQueryFacade = FacadesFactory.ReactiveDataQueryFacade;
            _logger = FacadesFactory.Logger;


            if (!_subscribed)
            {
                _subscribed = true;
                FacadesFactory.MessageBus.Listen<RecordChangedParam[]>().Subscribe(p =>
                {
                    var pushKey = DateTime.Now.Ticks.ToString();
                    var dataSources = string.Join(",", p.Select(_ => _.ProviderString));
                    var serializedMsg = JsonConvert.SerializeObject(p);

                    PushMessages.Add(pushKey, serializedMsg);
                    
                    // notify all clients about change
                    _reactiveDataHub.Value.Clients.All.UpdatesAreReady(pushKey, dataSources);                   
                });
            }
        }

        [BindJson(typeof(FilterRule[]), "filterRules")]
        [HttpPost]
        public HttpResponseMessage GetAggregatedData([FromUri]string userToken, [FromUri]string dataSourcePath, [FromBody]AggregatedWorksheetInfo aggregatedWorksheet,
            [FromUri]FilterRule[] filterRules)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetAggregatedData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetAggregatedData(userToken, dataSourcePath,
                    aggregatedWorksheet, filterRules), "GetAggregatedData failed ");
        }

        [BindJson(typeof(string[]), "fields")]
        [BindJson(typeof(FilterRule[]), "filterRules")]
        [HttpGet]
        public HttpResponseMessage GetData([FromUri]string userToken, [FromUri]string dataSourcePath, [FromUri]string[] fields, [FromUri]FilterRule[] filterRules)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetData(userToken, dataSourcePath, fields, filterRules), "GetData failed ");
        }

        [HttpGet]
        public HttpResponseMessage GetFieldData([FromUri]string userToken, [FromUri]string dataSourcePath, [FromUri] string field, [FromUri]string match, [FromUri] int elementsToReturn)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetFieldData");
            
            return EnumeratorToStream(_reactiveDataQueryFacade.GetFieldData(userToken, dataSourcePath, field, match, elementsToReturn), "GetFieldData failed ");
        }

        [HttpGet]
        public string GetPushMessage([FromUri]string userToken, [FromUri]string pushKey)
        {
            return (PushMessages.ContainsKey(pushKey)) ? PushMessages[pushKey] : string.Empty;
        }
     
        [BindJson(typeof(string[]), "fields")]
        [HttpPost]
        public HttpResponseMessage GetDataByKey([FromUri]string userToken, [FromUri] string dataSourcePath, [FromBody]string[] recordKeys, [FromUri]string[] fields)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetDataByKey");
           
            return EnumeratorToStream(_reactiveDataQueryFacade.GetDataByKey(userToken, dataSourcePath, recordKeys, fields), "GetDataByKey failed ");
        }

        [BindJson(typeof(string[]), "fields")]
        [HttpGet]
        public HttpResponseMessage GetDataChanges([FromUri]string userToken, [FromUri]string dataSourcePath, [FromUri]string[] fields)
        {
            _logger.Debug("Call ReactiveDataQueryApiController GetDataChanges");
            try
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.Error("GetDataChanges failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage ResolveRecordbyForeignKey([FromBody]RecordChangedParam[] changedRecord, [FromUri]string dataSourceUrn, [FromUri]string userToken)
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
        public bool CheckExistence([FromUri]string userToken, [FromUri]string dataSourceUrn, [FromUri]string fieldName, [FromUri]object value)
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
