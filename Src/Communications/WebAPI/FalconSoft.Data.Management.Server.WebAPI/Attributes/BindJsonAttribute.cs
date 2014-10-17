using System;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Script.Serialization;

namespace FalconSoft.Data.Management.Server.WebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class BindJsonAttribute : ActionFilterAttribute
    {
        readonly Type _type;
        readonly string _queryStringKey;
        private readonly object _syncObject;

        public BindJsonAttribute(Type type, string queryStringKey, object syncObject = null)
        {
            _type = type;
            _queryStringKey = queryStringKey;
            _syncObject = syncObject;
        }

        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (_syncObject != null)
            {
                lock (_syncObject)
                {
                    var json = actionContext.Request.RequestUri.ParseQueryString()[_queryStringKey];
                    var serializer = new JavaScriptSerializer();
                    actionContext.ActionArguments[_queryStringKey] = serializer.Deserialize(json, _type);
                }
            }
            else
            {
                var json = actionContext.Request.RequestUri.ParseQueryString()[_queryStringKey];
                var serializer = new JavaScriptSerializer();
                actionContext.ActionArguments[_queryStringKey] = json == null ? null : serializer.Deserialize(json, _type);
            }
        }
    }
}
