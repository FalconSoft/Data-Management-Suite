using System;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Script.Serialization;

namespace FalconSoft.Data.Management.Server.WebAPI.Attributes
{
    public class BindJsonAttribute : ActionFilterAttribute
    {
        readonly Type _type;
        readonly string _queryStringKey;
        public BindJsonAttribute(Type type, string queryStringKey)
        {
            _type = type;
            _queryStringKey = queryStringKey;
        }
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var json = actionContext.Request.RequestUri.ParseQueryString()[_queryStringKey];
            var serializer = new JavaScriptSerializer();
            actionContext.ActionArguments[_queryStringKey] = serializer.Deserialize(json, _type);
        }
    }
}
