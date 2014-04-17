using System;
using System.ComponentModel.Composition;
using FalconSoft.ReactiveWorksheets.Common.AttInterfaces;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
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
