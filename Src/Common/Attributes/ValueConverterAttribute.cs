using System;
using System.ComponentModel.Composition;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
    public interface IValueConverterAttribute
    {
        string Name { get; }

        DataTypes ToDataType { get; }
    }

    [MetadataAttribute]
    public class ValueConverterAttribute : Attribute, IValueConverterAttribute
    {
        public ValueConverterAttribute(string name, DataTypes toDataType)
        {
            Name = name;

            ToDataType = toDataType;
        }

        public string Name { get; private set; }

        public DataTypes ToDataType { get; private set; }
    }
}
