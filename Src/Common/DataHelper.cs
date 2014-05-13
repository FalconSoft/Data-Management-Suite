using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public static class DataHelper
    {
        public static object ToStrongTypedObject(this object value, string fieldName, DataSourceInfo dsInfo)
        {
            var dataType = dsInfo.Fields[fieldName].DataType;
            if (string.IsNullOrEmpty(value.ToString())) return null;
            switch (dataType)
            {
                case DataTypes.Int:
                    return string.IsNullOrEmpty(value.ToString()) ? 0 : Convert.ToInt32(value);
                case DataTypes.Double:
                    return string.IsNullOrEmpty(value.ToString()) ? 0.0 : Convert.ToDouble(value);
                case DataTypes.String:
                    return Convert.ToString(value);
                case DataTypes.Bool:
                    return !string.IsNullOrEmpty(value.ToString()) && Convert.ToBoolean(value);
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return string.IsNullOrEmpty(value.ToString()) ? new DateTime() : DateTime.ParseExact(value.ToString(), new[] { "dd/mm/yyyy", "d/mm/yyyy", "d/m/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }

        public static string WorkOutRecordKey(Dictionary<string, object> record, IEnumerable<string> keys)
        {
            if (!record.Any()) return string.Empty;
            return keys.Aggregate("", (current, key) => current + ("|" + record[key]));
        }
    }
}
