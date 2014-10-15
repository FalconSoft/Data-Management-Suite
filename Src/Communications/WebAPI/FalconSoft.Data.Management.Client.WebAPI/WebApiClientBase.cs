using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiClientBase
    {
        private HttpClient _client;

        public WebApiClientBase(string url)
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(url);
        }

        protected IEnumerable<T> GetEnumerableData<T>(string requestString, object[] inputParams)
        {
            return _client.GetAsync()
        } 
    }
}
