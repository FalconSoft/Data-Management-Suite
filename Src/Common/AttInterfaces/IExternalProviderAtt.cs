using System;

namespace FalconSoft.ReactiveWorksheets.Common.AttInterfaces
{
    public interface IExternalProviderAttribute
    {
        Type BaseType { get; }

        string[] KeyFieldNames { get; }
    }
}
