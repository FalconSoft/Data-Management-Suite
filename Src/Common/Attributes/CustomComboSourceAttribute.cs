using System;
using System.ComponentModel.Composition;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
    public interface ICustomComboSourceAttribute
    {
        string Name { get; }

        Type DataType { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomComboSourceAttribute : Attribute, ICustomComboSourceAttribute
    {

        public CustomComboSourceAttribute(string name, Type dataType)
        {
            Name = name;
            DataType = dataType;
        }

        public string Name { get; private set; }

        public Type DataType { get; private set; }
    }
}
