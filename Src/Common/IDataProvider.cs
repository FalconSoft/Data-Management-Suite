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
    public interface IDataProvider
    {
        event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null,
                                                        Action<string, string> onError = null);

        RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, 
            IEnumerable<string> recordsToDelete, string comment = null);
    }

    public class RevisionInfo
    {
        public bool IsSuccessfull { get; set; }

        public int RevisionId { get; set; }

        public string ErrorMessage { get; set; }
    }
}
