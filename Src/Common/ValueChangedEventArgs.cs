using System;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public class ValueChangedEventArgs : EventArgs
    {
        public bool IsDeleteAction { get; set; }

        public string DataSourceUrn { get; set; }

        public string[] ChangedPropertyNames { get; set; }

        public bool HasErrors { get; set; }

        public string ErrorMessage { get; set; }

        public object Value { get; set; }
    }

    public class ValueChangedEventArgs<T> : ValueChangedEventArgs
    {
        public new T Value { get; set; }
    }
}
