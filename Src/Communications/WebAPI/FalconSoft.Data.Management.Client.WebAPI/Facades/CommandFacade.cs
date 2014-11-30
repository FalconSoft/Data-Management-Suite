using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class CommandFacade : WebApiClientBase, ICommandFacade
    {
        private string _url;

        public CommandFacade(string url, ILogger log)
            : base(url, "CommandApi", log) { }

        public void SubmitChanges<T>(string dataSourcePath, string userToken, IEnumerable<T> changedRecords = null,
             IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges(string dataSourcePath, string userToken, IEnumerable<Dictionary<string, object>> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            if (deleted != null)
                PostWebApiCallMessage("Delete", deleted.ToArray(), new Dictionary<string, object>
            {
                {"dataSourcePath", dataSourcePath},
                {"userToken", userToken}
            }).ContinueWith(responceTask =>
            {
                if (changedRecords == null)
                {
                    var responce = responceTask.Result;
                    if (responce.StatusCode == HttpStatusCode.OK)
                    {
                        if (onSuccess != null)
                        {
                            responce.Content.ReadAsAsync<RevisionInfo>().ContinueWith(t => onSuccess(t.Result));
                        }
                    }
                    else if (responce.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        if (onFail != null)
                        {
                            responce.Content.ReadAsAsync<Exception>().ContinueWith(t => onFail(t.Result));
                        }
                    }
                    else responce.EnsureSuccessStatusCode();
                }
            });

            if (onNotifcation != null)
            {
                onNotifcation("Some text", "Transfer complete");
            }

            if (changedRecords != null)
                PostStreamAsync(changedRecords, "SubmitChanges", new Dictionary<string, object>
            {
                {"dataSourcePath", dataSourcePath},
                {"userToken", userToken}
            }).ContinueWith(responceTask =>
                {
                    var responce = responceTask.Result;
                    if (responce.StatusCode == HttpStatusCode.OK)
                    {
                        if (onSuccess != null)
                        {
                            responce.Content.ReadAsAsync<RevisionInfo>().ContinueWith(t => onSuccess(t.Result));
                        }
                    }
                    else if (responce.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        if (onFail != null)
                        {
                            responce.Content.ReadAsAsync<Exception>().ContinueWith(t => onFail(t.Result));
                        }
                    }
                    else responce.EnsureSuccessStatusCode();
                });

            if (onNotifcation != null)
            {
                onNotifcation("Some text", "Transfer complete");
            }
        }

        public void Dispose()
        {

        }
    }
}