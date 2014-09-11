using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using Microsoft.VisualBasic.FileIO;

namespace FalconSoft.Data.Server.SampleDataSources.ExternalSources
{
    public class BigDataSource : IDataProvider
    {
        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;
        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            if (!File.Exists(@"..\..\..\DataSources\SampleDataSources\Samples\300000x200.csv1"))
            {
                yield return new Dictionary<string, object>();
                yield break;
            }
            using (var parser = new TextFieldParser(@"..\..\..\DataSources\SampleDataSources\Samples\300000x200.csv"))
            {
                string separator = @",";
                parser.SetDelimiters(new[] { separator });
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TextFieldType = FieldType.Delimited;
                var header = parser.ReadLine().Split(separator.ToCharArray());

                while (!parser.EndOfData)
                {
                    var fields2 = parser.ReadFields();
                    var dict = new Dictionary<string, object>();

                    dict.Add(header[0], TryConvert(fields2[0], DataTypes.Int));
                    for (var i = 1; i < header.Count(); i++)
                    {
                        dict.Add(header[i], TryConvert(fields2[i], DataTypes.String));
                    }
                    yield return dict;
                }
            }
        }

        public DataTypes GetDataType(Type type)
        {
            switch (type.Name.ToLower())
            {
                case "string":
                    return DataTypes.String;

                case "int32":
                    return DataTypes.Int;

                default:
                    return DataTypes.String;
            }
        }


        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            throw new NotImplementedException();
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
    }

    public class BigData
    {
        public int ID{ get; set; }
        public string OrderID { get; set; }
        public string CustomerID { get; set; }
        public string EmployeeID { get; set; }
        public string OrderDate { get; set; }
        public string RequiredDate { get; set; }
        public string ShippedDate { get; set; }
        public string ShipVia { get; set; }
        public string Freight { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress  { get; set; }
        public string ShipCity  { get; set; }
        public string ShipRegion  { get; set; }
        public string ShipPostalCode  { get; set; }
        public string ShipCountry  { get; set; }
        public string Col16  { get; set; }
        public string Col17  { get; set; }
        public string Col18  { get; set; }
        public string Col19  { get; set; }
        public string Col20 { get; set; }
        public string Col21 { get; set; }
        public string Col22 { get; set; }
        public string Col23 { get; set; }
        public string Col24 { get; set; }
        public string Col25 { get; set; }
        public string Col26 { get; set; }
        public string Col27 { get; set; }
        public string Col28 { get; set; }
        public string Col29 { get; set; }
        public string Col30 { get; set; }
        public string Col31 { get; set; }
        public string Col32 { get; set; }
        public string Col33 { get; set; }
        public string Col34 { get; set; }
        public string Col35 { get; set; }
        public string Col36 { get; set; }
        public string Col37 { get; set; }
        public string Col38 { get; set; }
        public string Col39 { get; set; }
        public string Col40 { get; set; }
        public string Col41 { get; set; }
        public string Col42 { get; set; }
        public string Col43 { get; set; }
        public string Col44 { get; set; }
        public string Col45 { get; set; }
        public string Col46 { get; set; }
        public string Col47 { get; set; }
        public string Col48 { get; set; }
        public string Col49 { get; set; }
        public string Col50 { get; set; }
        public string Col51 { get; set; }
        public string Col52 { get; set; }
        public string Col53 { get; set; }
        public string Col54 { get; set; }
        public string Col55 { get; set; }
        public string Col56 { get; set; }
        public string Col57 { get; set; }
        public string Col58 { get; set; }
        public string Col59 { get; set; }
        public string Col60 { get; set; } 
        public string Col61 { get; set; } 
        public string Col62 { get; set; }
        public string Col63 { get; set; }
        public string Col64 { get; set; }
        public string Col65 { get; set; }
        public string Col66 { get; set; } 
        public string Col67 { get; set; } 
        public string Col68 { get; set; } 
        public string Col69 { get; set; } 
        public string Col70 { get; set; } 
        public string Col71 { get; set; } 
        public string Col72 { get; set; } 
        public string Col73 { get; set; } 
        public string Col74 { get; set; } 
        public string Col75 { get; set; } 
        public string Col76 { get; set; } 
        public string Col77 { get; set; } 
        public string Col78 { get; set; } 
        public string Col79 { get; set; } 
        public string Col80 { get; set; } 
        public string Col81 { get; set; } 
        public string Col82 { get; set; } 
        public string Col83 { get; set; } 
        public string Col84 { get; set; } 
        public string Col85 { get; set; } 
        public string Col86 { get; set; } 
        public string Col87 { get; set; } 
        public string Col88 { get; set; } 
        public string Col89 { get; set; } 
        public string Col90 { get; set; } 
        public string Col91 { get; set; } 
        public string Col92 { get; set; } 
        public string Col93 { get; set; } 
        public string Col94 { get; set; } 
        public string Col95 { get; set; } 
        public string Col96 { get; set; } 
        public string Col97 { get; set; } 
        public string Col98 { get; set; } 
        public string Col99 { get; set; } 
        public string Col100 { get; set; } 
        public string Col101 { get; set; } 
        public string Col102 { get; set; } 
        public string Col103 { get; set; } 
        public string Col104 { get; set; } 
        public string Col105 { get; set; } 
        public string Col106 { get; set; } 
        public string Col107 { get; set; } 
        public string Col108 { get; set; } 
        public string Col109 { get; set; } 
        public string Col110 { get; set; } 
        public string Col111 { get; set; } 
        public string Col112 { get; set; } 
        public string Col113 { get; set; } 
        public string Col114 { get; set; } 
        public string Col115 { get; set; } 
        public string Col116 { get; set; } 
        public string Col117 { get; set; } 
        public string Col118 { get; set; } 
        public string Col119 { get; set; } 
        public string Col120 { get; set; } 
        public string Col121 { get; set; } 
        public string Col122 { get; set; } 
        public string Col123 { get; set; } 
        public string Col124 { get; set; } 
        public string Col125 { get; set; } 
        public string Col126 { get; set; } 
        public string Col127 { get; set; } 
        public string Col128 { get; set; } 
        public string Col129 { get; set; } 
        public string Col130 { get; set; } 
        public string Col131 { get; set; } 
        public string Col132 { get; set; } 
        public string Col133 { get; set; } 
        public string Col134 { get; set; } 
        public string Col135 { get; set; } 
        public string Col136 { get; set; } 
        public string Col137 { get; set; }
        public string Col138 { get; set; } 
        public string Col139 { get; set; } 
        public string Col140 { get; set; } 
        public string Col141 { get; set; } 
        public string Col142 { get; set; } 
        public string Col143 { get; set; } 
        public string Col144 { get; set; } 
        public string Col145 { get; set; } 
        public string Col146 { get; set; } 
        public string Col147 { get; set; } 
        public string Col148 { get; set; } 
        public string Col149 { get; set; } 
        public string Col150 { get; set; } 
        public string Col151 { get; set; } 
        public string Col152 { get; set; } 
        public string Col153 { get; set; } 
        public string Col154 { get; set; } 
        public string Col155 { get; set; } 
        public string Col156 { get; set; } 
        public string Col157 { get; set; } 
        public string Col158 { get; set; } 
        public string Col159 { get; set; } 
        public string Col160 { get; set; } 
        public string Col161 { get; set; } 
        public string Col162 { get; set; } 
        public string Col163 { get; set; } 
        public string Col164 { get; set; } 
        public string Col165 { get; set; } 
        public string Col166 { get; set; } 
        public string Col167 { get; set; } 
        public string Col168 { get; set; } 
        public string Col169 { get; set; } 
        public string Col170 { get; set; } 
        public string Col171 { get; set; } 
        public string Col172 { get; set; } 
        public string Col173 { get; set; } 
        public string Col174 { get; set; } 
        public string Col175 { get; set; } 
        public string Col176 { get; set; } 
        public string Col177 { get; set; }
        public string Col178 { get; set; } 
        public string Col179 { get; set; }
        public string Col180 { get; set; }
        public string Col181 { get; set; }
        public string Col182 { get; set; }
        public string Col183 { get; set; }
        public string Col184 { get; set; }
        public string Col185 { get; set; }
        public string Col186 { get; set; }
        public string Col187 { get; set; }
        public string Col188 { get; set; }
        public string Col189 { get; set; }
        public string Col190 { get; set; }
        public string Col191 { get; set; }
        public string Col192 { get; set; }
        public string Col193 { get; set; }
        public string Col194 { get; set; }
        public string Col195 { get; set; }
        public string Col196 { get; set; }
        public string Col197 { get; set; }
        public string Col198 { get; set; }
        public string Col199 { get; set; }
        public string Col200 { get; set; }
    }
}