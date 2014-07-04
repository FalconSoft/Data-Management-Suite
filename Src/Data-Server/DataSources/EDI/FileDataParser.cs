using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Server.EDI.Feeds
{
   public class FileDataParser
    {
        public static IEnumerable<Dictionary<string, object>> ParseTxtToRows(string separator, string filePath)
        {
            string[] allLines;
            if (File.Exists(filePath))
            {
                allLines = File.ReadLines(filePath).ToArray();
            }
            else
            {
                yield break;
            }
            var fields = allLines[1].Split(separator.ToCharArray());
            IEnumerable<string> pointLines = allLines.Except(new[] { allLines.First(), allLines[1], allLines.Last() });
            foreach (string line in pointLines)
            {
                var dict = new Dictionary<string, object>();
                var values = line.Split(separator.ToCharArray());
                for (int i = 0; i < fields.Count(); i++)
                {
                    dict.Add(fields[i], values[i]);
                }
                yield return dict;
            }
        }

        public static IEnumerable<Dictionary<string, object>> ParseBySource(DataSourceInfo dataSourceInfo, IEnumerable<Dictionary<string, object>> data)
        {
            var fields = dataSourceInfo.Fields.Keys.ToArray();
            return data.Select(row => fields.ToDictionary(field => field, field => TryConvert(row[field].ToString(), dataSourceInfo.Fields[field].DataType)));
        }

        public static object TryConvert(string value, DataTypes type = DataTypes.Null)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (type != DataTypes.Null)
            {
                return ConvertFromDataType(value, type);
            }
            DataTypes supposedType;
            DateTime dateTime;
            int _int;
            double _double;
            bool _bool;
            if (DateTime.TryParse(value, out dateTime))
                supposedType = DataTypes.DateTime;
            else if (int.TryParse(value, out _int))
                supposedType = DataTypes.Int;
            else if (double.TryParse(value, out _double))
                supposedType = DataTypes.Double;
            else if (bool.TryParse(value, out _bool))
                supposedType = DataTypes.Double;
            else supposedType = DataTypes.String;

            return ConvertFromDataType(value, supposedType);
        }

        private static object ConvertFromDataType(string value, DataTypes type)
        {

            switch (type)
            {
                case DataTypes.String:
                    return value;
                case DataTypes.Int:
                    int i;
                    if (int.TryParse(value, out i))
                        return i;
                    return value;
                case DataTypes.Double:
                    double d;
                    if (double.TryParse(value, out d))
                        return d;
                    return value;
                case DataTypes.Date:
                case DataTypes.DateTime:
                    DateTime dt;
                    if (DateTime.TryParse(value, out dt))
                        return dt;
                    return value;
                case DataTypes.Bool:
                    bool b;
                    if (bool.TryParse(value, out b))
                        return b;
                    return value;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static IEnumerable<Dictionary<string, object>> ConvertDataToStrongTyped(IEnumerable<Dictionary<string, object>> data, DataSourceInfo dsInfo)
        {
            foreach (var row in data)
            {
                foreach (var fieldInfo in dsInfo.Fields)
                {
                    row[fieldInfo.Key] = TryConvert(row[fieldInfo.Key].ToString(),
                        dsInfo.Fields[fieldInfo.Key].DataType);
                }
                yield return row;
            }
        }
    }
}
