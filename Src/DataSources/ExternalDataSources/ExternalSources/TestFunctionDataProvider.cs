using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;

namespace ReactiveWorksheets.ExternalDataSources.ExternalSources
{
    public class TestFunctionDataProvider : IDataProvider
    {
        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            if (filterRules != null && filterRules.Length >= 2)
            {
                double inParam1 = double.Parse(filterRules[0].Value);
                double inParam2 = double.Parse(filterRules[1].Value);

                var result = new Dictionary<string, object>();
                result["in1"] = inParam1;
                result["in2"] = inParam1;

                result["out1"] = inParam1 + inParam2;
                result["out2"] = inParam1 - inParam2;
                result["out3"] = inParam1 * inParam2;
                result["out4"] = (inParam2 != 0.0)? inParam1 / inParam2 : 0;

                return new[] {result};
            }
            return new []{ new Dictionary<string, object>() };
        }

        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null)
        {
            return null;
        }

        public void UpdateSourceInfo(object sourceInfo)
        {
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
    }
}
