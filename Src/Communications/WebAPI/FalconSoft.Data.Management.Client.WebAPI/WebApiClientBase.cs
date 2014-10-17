using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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

        protected T GetWebApiCall<T>(string methodName)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            var response = _client.GetAsync(request.ToString()).Result;
            return response.Content.ReadAsAsync<T>().Result;
        }

        protected void PostWebApiCall(string methodName, Dictionary<string, object> inputParams)
        {
            //var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            //request.Append(ParametersToUriRequest(inputParams));

            //var response = _client.PostAsync(request.ToString()).Result;
        }

        protected IEnumerable<string> GetStreamData<T>(string methodName, Dictionary<string, object> inputParams)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(inputParams));

            var stream = _client.GetStreamAsync(request.ToString());
            var sr = new StreamReader(stream.Result);
            while (!sr.EndOfStream)
            {
                yield return sr.ReadLine();
            }
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
