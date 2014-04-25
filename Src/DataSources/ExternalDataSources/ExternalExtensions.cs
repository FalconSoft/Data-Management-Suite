﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FieldInfo = FalconSoft.ReactiveWorksheets.Common.Metadata.FieldInfo;

namespace ReactiveWorksheets.ExternalDataSources
{
    public static class ExternalExtensions
    {
        public static DataSourceInfo CreateDefaultDataSource(string[] keyFields, Type pocoType)
        {
            var fields = new List<FieldInfo>();

            EnumerateProperties((p, t, o) =>
            {
                var fieldInfo = new FieldInfo
                {
                    Name = p,
                    DataType = ToDataType(t),
                    IsNullable = true
                };
                if (keyFields.Contains(fieldInfo.Name))
                {
                    fieldInfo.IsKey = true;
                    fieldInfo.IsNullable = false;
                }
                fields.Add(fieldInfo);
            },
                pocoType.GetProperties(), string.Empty);
            return new DataSourceInfo(fields,true) { Name = pocoType.Name, Id = "-1", Category = "ExternalDataSource" };
        }

        private static void EnumerateProperties(Action<string, Type, object> setPropertyNameAction,
                                              IEnumerable<PropertyInfo> propertyInfos, string parentName, object value = null)
        {
            if (propertyInfos == null)
                return;

            foreach (var propInfo in propertyInfos)
            {


                string fieldName = (string.IsNullOrWhiteSpace(parentName))
                                       ? propInfo.Name
                                       : parentName + "." + propInfo.Name;
                if (fieldName == "Fields.Item")
                {
                    continue;
                }
                object propValue = (value != null) ? propInfo.GetValue(value, null) : null;

                if (
                    propInfo.PropertyType.IsValueType
                    || propInfo.PropertyType == typeof(string)
                    )
                {
                    setPropertyNameAction(fieldName, propInfo.PropertyType, propValue);
                }
                else if (propInfo.PropertyType.IsArray)
                {
                    // ignore for now
                }
                else
                {
                    EnumerateProperties(setPropertyNameAction, propInfo.PropertyType.GetProperties(), fieldName,
                                        propValue);
                }
            }
        }


        public static DataTypes ToDataType(Type t)
        {
            var type = Nullable.GetUnderlyingType(t);
            if (type != null)
            {
                if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                {
                    return DataTypes.Double;
                }
                if (type == typeof(DateTime))
                {
                    return DataTypes.DateTime;
                }
                if (type == typeof(bool))
                {
                    return DataTypes.Bool;
                }
                if (type == typeof(int))
                {
                    return DataTypes.Int;
                }
                return DataTypes.String;
            }
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal))
            {
                return DataTypes.Double;
            }
            if (t == typeof(DateTime))
            {
                return DataTypes.DateTime;
            }
            if (t == typeof(bool))
            {
                return DataTypes.Bool;
            }
            if (t == typeof(int))
            {
                return DataTypes.Int;
            }
            if (t == null)
            {
                return DataTypes.String;
            }
            return DataTypes.String;
        }
    }
}
