using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public enum AggregatedFunction
    {
        Count, Sum, Avg, Min, Max
    }

    /// <summary>
    /// main data query facade to query Live Data from Reactive Worksheets Data Repository 
    /// </summary>
    public interface IReactiveDataQueryFacade : IDisposable
    {
        /// <summary>
        /// Get Aggregated Data Source
        /// </summary>
        /// <param name="dataSourcePath">data source path</param>
        /// <param name="aggregatedWorksheet">Structure to aggregate by</param>
        /// <param name="filterRules">filter before aggregation</param>
        /// <returns>Returns Aggregated result</returns>
        IEnumerable<Dictionary<string, object>> GetAggregatedData(string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null);

        /// <summary>
        /// Method to query Live Data from Reactive Worksheets Data Repository
        /// </summary>
        /// <typeparam name="T">strong type (a simple conversion from Dictionary(string, object))</typeparam>
        /// <param name="dataSourcePath">Data Source Path</param>
        /// <param name="filterRules">Set of filter rules</param>
        /// <returns>Returns all data for specific data source based on filter rules</returns>
        IEnumerable<T> GetData<T>(string dataSourcePath, FilterRule[] filterRules = null);

        /// <summary>
        /// Method to query Live Data from Reactive Worksheets Data Repository
        /// </summary>
        /// <param name="dataSourcePath">Data Source Path</param>
        /// <param name="filterRules">Set of filter rules</param>
        /// <returns>Returns all data for specific data source based on filter rules</returns>
        IEnumerable<Dictionary<string, object>> GetData(string dataSourcePath, FilterRule[] filterRules = null);

        /// <summary>
        /// Cold observable to observe data changes for the specific data source and filtered by FilterRules
        /// </summary>
        /// <param name="dataSourcePath">Data Source path to listen changes to</param>
        /// <param name="filterRules">Filter rules</param>
        /// <returns>cold observable with changes</returns>
        IObservable<RecordChangedParam> GetDataChanges(string dataSourcePath, FilterRule[] filterRules = null);

        /// <summary>
        /// Method to resolve related fields in case when foreign key is changed.
        /// This method doesn't change any state on the server and is used only on 
        /// client to recalculate related fields 
        /// </summary>
        /// <param name="changedRecord">Changed record with foreignKey</param>
        /// <param name="onSuccess">Action called when fields have been resolved</param>
        /// <param name="onFail">Resolve failed</param>
        void ResolveRecordbyForeignKey(RecordChangedParam changedRecord,
            Action<string, RecordChangedParam> onSuccess,
            Action<string, Exception> onFail);

    }
}
