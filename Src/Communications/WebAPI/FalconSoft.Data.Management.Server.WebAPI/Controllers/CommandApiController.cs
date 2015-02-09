using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class CommandApiController : ApiController
    {
        private readonly ICommandFacade _commandFacade;
        private readonly ILogger _logger;

        public CommandApiController()
        {
            _commandFacade = FacadesFactory.CommandFacade;
            _logger = FacadesFactory.Logger;
        }

        [HttpPost]
        public HttpResponseMessage Delete(
            [FromUri] string dataSourcePath,
            [FromUri] string userToken)
        {
            _logger.Debug("Call CommandApiController Delete");
            try
            {
                var responce = new HttpResponseMessage();

                var deleted = Request.Content.ReadAsAsync<string[]>().Result;
                _commandFacade.SubmitChanges(dataSourcePath, userToken, null, deleted,
                    rInfo =>
                    {
                        responce.StatusCode = HttpStatusCode.OK;
                        responce.Content = new ObjectContent(typeof(RevisionInfo), rInfo, new JsonMediaTypeFormatter());
                    }, ex =>
                    {
                        responce.StatusCode = HttpStatusCode.InternalServerError;
                        responce.Content = new ObjectContent(typeof(Exception), ex, new JsonMediaTypeFormatter());
                    });
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("SubmitChanges failed.", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }


        [HttpPost]
        public HttpResponseMessage SubmitChanges( 
            [FromUri] string dataSourcePath,
            [FromUri] string userToken)
        {
            _logger.Debug("Call CommandApiController SubmitChanges");
            try
            {
                var responce = new HttpResponseMessage();

                var request = Request.Content.ReadAsStreamAsync().Result;
                _commandFacade.SubmitChanges(dataSourcePath, userToken,
                    StreamToEnumerable<Dictionary<string, object>>(request), null,
                    rInfo =>
                    {
                        responce.StatusCode = HttpStatusCode.OK;
                        responce.Content = new ObjectContent(typeof (RevisionInfo), rInfo, new JsonMediaTypeFormatter());
                    }, ex =>
                    {
                        responce.StatusCode = HttpStatusCode.InternalServerError;
                        responce.Content = new ObjectContent(typeof (Exception), ex, new JsonMediaTypeFormatter());
                    });
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("SubmitChanges failed.", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private IEnumerable<T> StreamToEnumerable<T>(Stream stream)
        {
            var jsonSerializer = new JavaScriptSerializer();
            var sr = new StreamReader(stream);

            var lastItem = string.Empty;

            while (!sr.EndOfStream)
            {
                var outPutString = lastItem + sr.ReadLine();

                if (string.IsNullOrEmpty(outPutString))
                    continue;

                var array = outPutString.Split(new[] {"[#]"}, StringSplitOptions.None);

                var itemCount = array.Length;
                lastItem = array[itemCount - 1];

                for (int i = 0; i < itemCount - 1; i++)
                {
                    yield return jsonSerializer.Deserialize<T>(array[i]);
                }
            }
            if (!string.IsNullOrEmpty(lastItem))
                yield return jsonSerializer.Deserialize<T>(lastItem);
        }
    }
}
