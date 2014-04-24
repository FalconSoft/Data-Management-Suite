using System;
using System.Collections;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    /// <summary>
    /// Is a main interface what describes a basic data provider functionality
    /// This interface has to be registered before it will consumed with ReactiveWorksheets.
    /// </summary>
    public interface IDataProvider : IBaseProvider
    {
        IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null,
                                                        Action<string, string> onError = null);

        RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null);

        void UpdateSourceInfo(object sourceInfo);
    }

    public interface IServiceDataProvider : IBaseProvider
    {

        /// <summary>
        /// Definition of synchronous method to perform calculations.
        /// This method will be called when server starts and when any of "in" Parameters will change.
        /// In future, we will add more options to control over calls to this method  
        /// </summary>
        /// <param name="inParams">input parameters</param>
        /// <param name="onError">error callback(string RecordKey, string errorMessage). Will be called every time when error occurs</param>
        /// <returns></returns>
        IEnumerable<Dictionary<string, object>> GetData(IEnumerable<Dictionary<string, object>> inParams, Action<string, string> onError = null);

        // remove this method in favour to above one
        void RequestCalculation(DataSourceInfo dataSourceInfo, RecordChangedParam recordChangedParam, Action<string, RecordChangedParam> onSuccess,
            Action<string, Exception> onFail);
    }


    public class RevisionInfo
    {
        public bool IsSuccessfull { get; set; }

        public int RevisionId { get; set; }

        public string ErrorMessage { get; set; }
    }
}
