using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public static class DataHelper
    {

        public static object ToStrongTypedObject(string fieldName, object value, DataSourceInfo dsInfo)
        {
            var dataType = dsInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            switch (dataType)
            {
                case DataTypes.Int:
                    return Convert.ToInt32(value);
                case DataTypes.Double:
                    return Convert.ToDouble(value);
                case DataTypes.String:
                    return Convert.ToString(value);
                case DataTypes.Bool:
                    return Convert.ToBoolean(value);
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return Convert.ToDateTime(value);
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }

        public static object ToStrongTypedObject(this object value, string fieldName, DataSourceInfo dsInfo)
        {
            var dataType = dsInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            switch (dataType)
            {
                case DataTypes.Int:
                    return Convert.ToInt32(value);
                case DataTypes.Double:
                    return Convert.ToDouble(value);
                case DataTypes.String:
                    return Convert.ToString(value);
                case DataTypes.Bool:
                    return Convert.ToBoolean(value);
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return DateTime.ParseExact(value.ToString(), new[] { "dd/mm/yyyy", "d/mm/yyyy", "d/m/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
                   // return Convert.ToDateTime(Convert.ToString(value));
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
