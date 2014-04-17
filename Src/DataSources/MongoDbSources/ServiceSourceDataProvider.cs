using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources
{
    public class ServiceSourceDataProvider : IServiceDataProvider
    {
        private readonly List<Dictionary<string, object>> _collection = new List<Dictionary<string, object>>();
        public ServiceSourceInfo ServiceSourceInfo;
        private readonly object _lock = new object();
        
        public void RequestCalculation(IList inputParameters)
        {
            lock (_lock)
            {
                _collection.Clear();
                //foreach (var inputParameter in inputParameters)
                //{
                //    if ((inputParameter as IList<KeyValuePair<string, object>>) == null) return;
                //    _collection.Add(
                //        (inputParameter as IList<KeyValuePair<string, object>>).ToDictionary(
                //            parameter => parameter.Key, parameter => parameter.Value));
                //}

                //foreach (var x in _collection)
                //{
                //    var results = new PythonEngine().GetFormulaResult(ServiceSourceInfo.Script, x,
                //        ServiceSourceInfo.OutParams.ToDictionary(y => y.Name, y => y.Value));
                //    if (results == null)
                //    {
                //        if (RecordChangedEvent != null)
                //            RecordChangedEvent(this,
                //                new ValueChangedEventArgs
                //                {
                //                    DataSourceUrn = string.Format(@"{0}\{1}", ServiceSourceInfo.Category, ServiceSourceInfo.Name),
                //                    Value = null,
                //                    ErrorMessage = "Convert Error",
                //                    ChangedPropertyNames = ServiceSourceInfo.OutParams.Select(y => y.Name).ToArray()
                //                });
                //    }
                //    else if (results.ContainsKey(string.Empty))
                //    {
                //        if (RecordChangedEvent != null)
                //            RecordChangedEvent(this,
                //                new ValueChangedEventArgs
                //                {
                //                    DataSourceUrn = string.Format(@"{0}\{1}", ServiceSourceInfo.Category, ServiceSourceInfo.Name),
                //                    Value = null,
                //                    ErrorMessage = results[""].Error,
                //                    ChangedPropertyNames = ServiceSourceInfo.OutParams.Select(y => y.Name).ToArray()
                //                });
                //    }
                //    else
                //    {
                //        foreach (var pythonResult in results)
                //        {
                //            x.Add(pythonResult.Key, pythonResult.Value.ResultValue);
                //        }
                //        if (RecordChangedEvent != null)
                //            RecordChangedEvent(this,
                //                new ValueChangedEventArgs
                //                {
                //                    DataSourceUrn = string.Format(@"{0}\{1}", ServiceSourceInfo.Category, ServiceSourceInfo.Name),
                //                    Value = x,
                //                    ChangedPropertyNames = ServiceSourceInfo.OutParams.Select(y => y.Name).ToArray()
                //                });
                //    }
                //}
            }
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
    }
}
