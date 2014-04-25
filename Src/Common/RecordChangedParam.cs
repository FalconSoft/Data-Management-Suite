using System;
using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public enum RecordChangedAction
    {
        AddedOrUpdated,

        Removed
    }

    public class RecordChangedParam : EventArgs
    {
        public string ChangeSource { get; set; }

        public string UserToken { get; set; }

        public string IgnoreWorksheet { get; set; }

        public RecordChangedAction ChangedAction { get; set; }

        public string ProviderString { get; set; }

        public string[] ChangedPropertyNames { get; set; }

        public string RecordKey { get; set; }

        public IDictionary<string, object> RecordValues { get; set; }

        public string OriginalRecordKey { get; set; }
    }
}
