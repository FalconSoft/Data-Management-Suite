using System;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface IBaseProvider
    {
        event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
    }
}
