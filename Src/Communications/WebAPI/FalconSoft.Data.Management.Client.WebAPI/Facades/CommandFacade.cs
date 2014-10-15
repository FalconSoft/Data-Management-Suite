using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class CommandFacade : ICommandFacade
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges<T>(string dataSourcePath, string userToken, IEnumerable<T> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            throw new NotImplementedException();
        }

        public void SubmitChanges(string dataSourcePath, string userToken, IEnumerable<Dictionary<string, object>> changedRecords = null,
            IEnumerable<string> deleted = null, Action<RevisionInfo> onSuccess = null, Action<Exception> onFail = null, Action<string, string> onNotifcation = null)
        {
            throw new NotImplementedException();
        }
    }
}