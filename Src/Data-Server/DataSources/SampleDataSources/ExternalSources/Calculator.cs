using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;

namespace FalconSoft.Data.Server.SampleDataSources.ExternalSources
{
    public class CalculatorDataProvider : IDataProvider
    {
        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            if (filterRules != null && filterRules.Length >= 1)
            {
                double inParam1 = (!string.IsNullOrWhiteSpace(filterRules[0].Value))? double.Parse(filterRules[0].Value) : 0;
                double inParam2 = (!string.IsNullOrWhiteSpace(filterRules[1].Value))? double.Parse(filterRules[1].Value) : 0;

                var result = new Dictionary<string, object>();
                result["in1"] = inParam1;
                result["in2"] = inParam1;

                result["Out1"] = inParam1 + inParam2;
                result["Out2"] = inParam1 - inParam2;
                result["Out3"] = inParam1 * inParam2;
                result["Out4"] = (inParam2 != 0.0) ? inParam1 / inParam2 : 0;
                result["Time"] = DateTime.Now;

                return new[] { result };
            }
            return new List<Dictionary<string, object>>();
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, 
            IEnumerable<string> recordsToDelete, 
            string comment = null)
        {
            return null;
        }

        public void UpdateSourceInfo(object sourceInfo)
        {
           
        }
    }



    public class Calculator
    {
        public double In1 { get; set; }
        
        public double In2 { get; set; }

        public double Out1 { get; set; }

        public double Out2 { get; set; }

        public double Out3 { get; set; }

        public double Out4 { get; set; }

        public DateTime Time { get; set; }
    }
}
