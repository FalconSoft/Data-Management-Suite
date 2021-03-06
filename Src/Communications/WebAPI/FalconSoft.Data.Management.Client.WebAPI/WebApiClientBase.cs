﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiClientBase : IServerNotification
    {
        private readonly string _apiControllerName;
        private readonly HttpClient _client;
        protected readonly ILogger _log;

        public WebApiClientBase(string url, string apiControllerName, ILogger log)
        {
            _log = log;
            _apiControllerName = apiControllerName;
            _client = new HttpClient {BaseAddress = new Uri(url)};
        }

        protected T GetWebApiCall<T>(string methodName, Dictionary<string, object> inputParams)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));
            
            request.Append(ParametersToUriRequest(inputParams));

            DateTime startTime = DateTime.Now;
            var response = _client.GetAsync(request.ToString()).Result;
            var result = response.Content.ReadAsAsync<T>().Result;

            _log.DebugFormat("WebApi get [{0}] in {1}", request, DateTime.Now - startTime);
            return result;
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

            DateTime startTime = DateTime.Now;
            var response = _client.GetAsync(request.ToString()).Result;
            var result = response.Content.ReadAsAsync<T>().Result;

            _log.DebugFormat("WebApi get [{0}] in {1}", request, DateTime.Now - startTime);

            return result;
        }

        protected void PostWebApiCall<T>(string methodName, T bodyElenment, Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));
            DateTime startTime = DateTime.Now;

            var response = _client.PostAsJsonAsync(request.ToString(), bodyElenment).Result;
            response.EnsureSuccessStatusCode();

            _log.DebugFormat("WebApi post [{0}] in {1}", request, DateTime.Now - startTime);
        }

        protected Task<HttpResponseMessage> PostWebApiCallMessage<T>(string methodName, T bodyElenment, Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));

            var response = _client.PostAsJsonAsync(request.ToString(), bodyElenment);
           
            return response;
        }

        protected void PostWebApiCall<T>(string methodName, T bodyElenment)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            DateTime startTime = DateTime.Now;
            var response = _client.PostAsJsonAsync(request.ToString(), bodyElenment).Result;
            response.EnsureSuccessStatusCode();
            _log.DebugFormat("WebApi post [{0}] in {1}", request, DateTime.Now - startTime);
        }

        protected TOutT PostWithResultWebApiCall<TInT, TOutT>(string methodName, TInT bodyElenment)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            DateTime startTime = DateTime.Now;
            var response = _client.PostAsJsonAsync(request.ToString(), bodyElenment).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsAsync<TOutT>().Result;
            _log.DebugFormat("WebApi post with result [{0}] in {1}", request, DateTime.Now - startTime);

            return result;
        }

        protected void PostWebApiCall(string methodName, Dictionary<string, object> uriParams)
        {
            var request = new StringBuilder(string.Format("api/{0}/{1}/", _apiControllerName, methodName));

            request.Append(ParametersToUriRequest(uriParams));

            DateTime startTime = DateTime.Now;
            var response = _client.PostAsync(request.ToString(), new StringContent(methodName)).Result;
            response.EnsureSuccessStatusCode();

            _log.DebugFormat("WebApi post [{0}] in {1}", request, DateTime.Now - startTime);
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

        protected IEnumerable<T> GetStreamDataToEnumerable<T,TM>(string methodName, TM body, Dictionary<string, object> inputParams, bool allowReadStreamBuffering = true)
        {
            var requesturl = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));
            
            DateTime startTime = DateTime.Now;
            var jsonSerializer = new JavaScriptSerializer();
            requesturl.Append(ParametersToUriRequest(inputParams));

            var responce = _client.PostAsync(requesturl.ToString(), new ObjectContent(typeof(TM), body, new JsonMediaTypeFormatter())).Result;

            try
            {
                responce.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                yield break;
            }

            var sr = new StreamReader(responce.Content.ReadAsStreamAsync().Result);

            var lastItem = string.Empty;

            _log.DebugFormat("WebApi streaming {0} in {1} after started", requesturl.ToString(), DateTime.Now - startTime);


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

        protected IEnumerable<T> GetStreamDataToEnumerable<T>(string methodName, Dictionary<string, object> inputParams, bool allowReadStreamBuffering = true)
        {
            var request = new StringBuilder(string.Format(@"api/{0}/{1}/", _apiControllerName, methodName));
            var jsonSerializer = new JavaScriptSerializer();
            DateTime startTime = DateTime.Now;
            request.Append(ParametersToUriRequest(inputParams));

            var stream = _client.GetStreamAsync(request.ToString());
            var sr = new StreamReader(stream.Result);

            _log.DebugFormat("WebApi streaming {0} in {1} after started", request.ToString(), DateTime.Now - startTime);
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
                    foreach (var dictionary in dataEnumerable??Enumerable.Empty<T>())
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

        #region Rabbit MQ

        public IObservable<T> CreateExchngeObservable<T>(string exchangeName,
            string exchangeType, string routingKey)
        {
           return new Subject<T>(); //_rabbitMQClient.CreateExchngeObservable<T>(exchangeName, exchangeType, routingKey);
        }
        #endregion

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


        public event EventHandler<ServerErrorEvArgs> ServerErrorHandler;
    //{
    //        add { if (_rabbitMQClient != null) ((IServerNotification) _rabbitMQClient).ServerErrorHandler += value; }
    //        remove { if (_rabbitMQClient != null) ((IServerNotification)_rabbitMQClient).ServerErrorHandler -= value; }
    //    }

        public event EventHandler<ServerReconnectionArgs> ServerReconnectedEvent;
    //{
    //    add
    //    {
    //        if (_rabbitMQClient != null) ((IServerNotification) _rabbitMQClient).ServerReconnectedEvent += value;
    //    }
    //    remove
    //    {
    //        if (_rabbitMQClient != null) ((IServerNotification) _rabbitMQClient).ServerReconnectedEvent -= value;
    //    }
    //}
    }
}
