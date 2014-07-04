using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class PythonDataProvider : IDataProvider
    {
       
        public PythonDataProvider(DataSourceInfo dataSourceInfo)
        {
            DataSourceInfo = dataSourceInfo;
        }

        public DataSourceInfo DataSourceInfo { get; private set; }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            var pythonEngine = new PythonEngine.PythonEngine();

            if ((filterRules != null) && filterRules.Any())
            {
                var resultList = new List<Dictionary<string, object>>();
               resultList.Add(pythonEngine.GetScriptResult(DataSourceInfo.Description, filterRules.ToDictionary(fr => fr.FieldName,  fr => CastToType(DataSourceInfo.Fields[fr.FieldName].DataType, fr.Value))));

                return resultList;

            }
           return  new List<Dictionary<string, object>>();
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            return null;
        }

        private static object CastToType(DataTypes dataType, object value)
        {
            switch (dataType)
            {
                case DataTypes.Int:
                    if (!(value is int))
                        return Convert.ToInt32(value);
                    break;
                case DataTypes.Double:
                    if (!(value is double))
                        return Convert.ToDouble(value);
                    break;
                case DataTypes.String:
                    if (!(value is string))
                        return Convert.ToString(value);
                    break;
                case DataTypes.Bool:
                    if (!(value is int))
                        return Convert.ToInt32(value);
                    break;
                case DataTypes.Date:
                case DataTypes.DateTime:
                    if (!(value is DateTime))
                        return Convert.ToDateTime(value);
                    break;
                default:
                    return value;
            }
            return value;
        }
    }
}