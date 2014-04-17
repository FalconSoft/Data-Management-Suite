using System;
using System.ComponentModel.Composition;
using FalconSoft.ReactiveWorksheets.Common.AttInterfaces;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomComboSourceAttribute : Attribute, ICustomComboSourceAtribute
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
