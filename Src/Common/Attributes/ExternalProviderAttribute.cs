using System;
using System.ComponentModel.Composition;

namespace FalconSoft.ReactiveWorksheets.Common.Attributes
{
    public interface IExternalProviderAttribute
    {
        Type BaseType { get; }

        string[] KeyFieldNames { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ExternalProviderAttribute : Attribute, IExternalProviderAttribute
    {
        public ExternalProviderAttribute(Type baseType, string[] keyFieldNames)
        {
            BaseType = baseType;
            KeyFieldNames = keyFieldNames;
        }

        public Type BaseType { get; private set; }

        public string[] KeyFieldNames { get; private set; }
    }
}
