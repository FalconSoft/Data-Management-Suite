using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.VisualBasic.FileIO;

namespace FalconSoft.ReactiveWorksheets.Console.Client
{
    public static class CSVHelper
    {
        public static IEnumerable<Dictionary<string, object>> ReadRecords(DataSourceInfo dsInfo, string fileName, string separator)
        {
            var result = new List<Dictionary<string, object>>();
            
            using (var parser = new TextFieldParser(fileName))
            {
                parser.SetDelimiters(new[] { separator });
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TextFieldType = FieldType.Delimited;
                var header = parser.ReadLine().Split(separator.ToCharArray());
                
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    var dict = new Dictionary<string, object>();
                    for (var i = 0; i < header.Count(); i++)
                    {
                        dict.Add(header[i], TryConvert(fields[i], dsInfo.Fields[header[i]].DataType));
                    }
                    result.Add(dict);
                }
            }
            return result;
        }

        public static bool WriteRecords(IEnumerable<IDictionary<string, object>> data, string fileName, string separator, bool append = false)
        {
            var recordsSb = new StringBuilder();
            recordsSb.AppendLine(string.Join(separator, data.First().Keys));

            foreach (var record in data)
            {
                recordsSb.AppendLine(string.Join(separator, record.Values));
            }

            using (var streamWriter = new StreamWriter(fileName, append))
            {
                streamWriter.Write(recordsSb);    
            }
            
            return true;
        }

        public static IEnumerable<string> ReadRecordsToDelete(string fileName)
        {
            using (var streamReader = new StreamReader(fileName))
            {
                var recordsKyToDelete = streamReader.ReadToEnd();

                return recordsKyToDelete.Split("\r\n".ToCharArray());
            }
        }


        private static object TryConvert(string value, DataTypes type = DataTypes.Null)
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
            try
            {
                switch (type)
                {
                    case DataTypes.String:
                        return value;
                    case DataTypes.Int:
                        return Convert.ToInt32(value);
                    case DataTypes.Double:
                        return Convert.ToDouble(value);
                    case DataTypes.Date:
                    case DataTypes.DateTime:
                        return Convert.ToDateTime(value);
                    case DataTypes.Bool:
                        return Convert.ToBoolean(value);
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }
            }
            catch (InvalidCastException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            return value;
        }
    }
}