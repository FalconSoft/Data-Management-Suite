using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using Newtonsoft.Json;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiClientBase
    {
        private readonly string _apiControllerName;
        private readonly HttpClient _client;

        public WebApiClientBase(string url, string apiControllerName)
        {
            _apiControllerName = apiControllerName;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(url);
        }

        protected T GetWebApiCall<T>(string methodName, Dictionary<string, object> inputParams)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(inputParams));

            var response = _client.GetAsync(request.ToString()).Result;
            return response.Content.ReadAsAsync<T>().Result;
        }

        protected Task<HttpResponseMessage> GetWebApiAsyncCall(string methodName, Dictionary<string, object> inputParams)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(inputParams));

            return _client.GetAsync(request.ToString());
        }

        protected T GetWebApiCall<T>(string methodName)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            var response = _client.GetAsync(request.ToString()).Result;
            return response.Content.ReadAsAsync<T>().Result;
        }

        protected void PostWebApiCall<T>(string methodName, T bodyElenment, Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));

            var response = _client.PostAsJsonAsync(request.ToString(), bodyElenment).Result;
            response.EnsureSuccessStatusCode();
        }

        protected void PostWebApiCall(string methodName, Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));

            var response = _client.PostAsync(request.ToString(), new StringContent(methodName)).Result;
            response.EnsureSuccessStatusCode();
        }

        protected void ResolveRecordbyForeignKeyGet(RecordChangedParam[] changedRecord, string dataSourceUrn,
            string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, "ResolveRecordbyForeignKey"));

            var inputParams = new Dictionary<string, object>
            {
                {"changedRecord", changedRecord},
                {"dataSourceUrn", dataSourceUrn},
                {"userToken", userToken}
            };

            request.Append(ParametersToUriRequest(inputParams));

            _client.GetAsync(request.ToString()).ContinueWith(completeTask =>
            {
                var responce = completeTask.Result;

                if (responce.StatusCode == HttpStatusCode.OK)
                {
                    if (onSuccess != null)
                    {
                        responce.Content
                            .ReadAsAsync<RecordChangedParam[]>()
                            .ContinueWith(t => onSuccess(responce.ReasonPhrase, t.Result));
                    }
                }
                else if (responce.StatusCode == HttpStatusCode.InternalServerError)
                {
                    if (onFail != null)
                    {
                        responce.Content
                            .ReadAsAsync<Exception>()
                            .ContinueWith(t => onFail(responce.ReasonPhrase, t.Result));
                    }
                }
                else responce.EnsureSuccessStatusCode();
            });

        }

        protected IEnumerable<T> GetStreamDataToEnumerable<T>(string methodName, Dictionary<string, object> inputParams, bool allowReadStreamBuffering = true)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));
            var jsonSerializer = new JavaScriptSerializer();
            request.Append(ParametersToUriRequest(inputParams));

            var stream = _client.GetStreamAsync(request.ToString());
            var sr = new StreamReader(stream.Result);

            var lastItem = string.Empty;

            while (!sr.EndOfStream)
            {
                var outPutString = lastItem + sr.ReadLine();

                if (string.IsNullOrEmpty(outPutString))
                    continue;

                var array = outPutString.Split(new[] { "[#]" }, StringSplitOptions.None);

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

        protected Task<HttpResponseMessage> PostStreamAsync<T>(IEnumerable<T> dataEnumerable, string methodName,
            Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));

            var content = new PushStreamContent((stream, httpContent, transportContext) => new Task(() =>
            {
                try
                {
                    // Execute the command and get a reader
                    var sw = new BinaryWriter(stream);
                    var jsonConverter = new JavaScriptSerializer();
                    foreach (var dictionary in dataEnumerable)
                    {
                        var json = jsonConverter.Serialize(dictionary) + "[#]";

                        var buffer = Encoding.UTF8.GetBytes(json);

                        // Write out data to output stream
                        sw.Write(buffer);
                    }
                }
                finally
                {
                    stream.Close();
                }
            }).Start());

            return _client.PostAsync(request.ToString(), content);
        }

        private string ParametersToUriRequest(Dictionary<string, object> inputParams)
        {
            var request = new StringBuilder();
            var jsonSerializer = new JavaScriptSerializer();

            foreach (var inputParam in inputParams)
            {
                request.Append("&");

                if (inputParam.Value == null)
                {
                    request.AppendFormat("{0}={1}", inputParam.Key, inputParam.Value);
                    continue;

                }

                if (inputParam.Value is string)
                {
                    request.AppendFormat("{0}={1}", inputParam.Key, inputParam.Value);
                    continue;
                }

                var type = inputParam.Value.GetType();

                if (type.IsValueType)
                {
                    request.AppendFormat("{0}={1}", inputParam.Key, inputParam.Value);
                    continue;
                }

                if (type.IsArray)
                {
                    request.AppendFormat("{0}={1}", inputParam.Key, jsonSerializer.Serialize(inputParam.Value));
                    continue;
                }

                if (type.IsClass)
                {
                    request.AppendFormat("{0}={1}", inputParam.Key, jsonSerializer.Serialize(inputParam.Value));
                }
            }

            if (request.Capacity > 0)
            {
                request.Remove(0, 1);
                request.Insert(0, "?");
            }
            return request.ToString();
        }
    }
}
