using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class CalculatorDataProvider:IDataProvider
    {
        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            if (filterRules != null && filterRules.Length >= 1)
            {
                double inParam1 = double.Parse(filterRules[0].Value);
                const double inParam2 = 100; //double.Parse(filterRules[1].Value);

                var result = new Dictionary<string, object>();
                result["in1"] = inParam1;
               // result["in2"] = inParam1;

                result["Out1"] = inParam1 + inParam2;
                result["Out2"] = inParam1 - inParam2;
                result["Out3"] = inParam1 * inParam2;
                result["Out4"] = (inParam2 != 0.0) ? inParam1 / inParam2 : 0;

                return new[] { result };
            }
            return new List<Dictionary<string, object>>();
        }

        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null)
        {
            return null;
        }

        public void UpdateSourceInfo(object sourceInfo)
        {
           
        }
    }



    public class Calculator
    {
        public double Key { get; set; }

        public double Out1 { get; set; }

        public double Out2 { get; set; }

        public double Out3 { get; set; }

        public double Out4 { get; set; }
    }
}
