using System;
using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    /// <summary>
    /// Commands Facade defines methods what are changing Reactive Worksheets server's state.
    /// </summary>
    public interface ICommandFacade : IDisposable
    {
        /// <summary>
        /// Submit changed records to ReactiveWorksheets server, which will update original source as well as Data Repository
        /// </summary>
        /// <typeparam name="T">Strong type what will be converted to Dictionary(string, object) </typeparam>
        /// <param name="dataSourcePath">Data Source path</param>
        /// <param name="comment">Comment</param>
        /// <param name="changedRecords">records to update or insert</param>
        /// <param name="deleted">RecordKeys to delete</param>
        /// <param name="onSuccess">Action called when on success, where string is a RecordKey and Revision</param>
        /// <param name="onFail">Action called on fail</param>
        void SubmitChanges<T>(string dataSourcePath, string comment,
            IEnumerable<T> changedRecords = null,
            IEnumerable<string> deleted = null,
            Action<RevisionInfo> onSuccess = null,
            Action<Exception> onFail = null);

        /// <summary>
        /// Submit changed records to ReactiveWorksheets server, which will update original source as well as Data Repository
        /// </summary>
        /// <param name="dataSourcePath">Data Source path</param>
        /// <param name="comment">Comment</param>
        /// <param name="changedRecords">records to update or insert</param>
        /// <param name="deleted">records to delete</param>
        /// <param name="onSuccess">Action called when on success, where string is a RecordKey and Revision</param>
        /// <param name="onFail">Action called on fail</param>
        void SubmitChanges(string dataSourcePath, string comment,
            IEnumerable<Dictionary<string, object>> changedRecords = null,
            IEnumerable<string> deleted = null,
            Action<RevisionInfo> onSuccess = null,
            Action<Exception> onFail = null);
    }
}
