using System;
using System.Collections;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface IServiceDataProvider : IBaseProvider
    {
        void RequestCalculation(DataSourceInfo dataSourceInfo, RecordChangedParam recordChangedParam, Action<string, RecordChangedParam> onSuccess,
            Action<string, Exception> onFail);
    }
}
