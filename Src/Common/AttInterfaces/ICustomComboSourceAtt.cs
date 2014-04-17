using System;

namespace FalconSoft.ReactiveWorksheets.Common.AttInterfaces
{
    public interface ICustomComboSourceAtribute
    {
        string Name { get; }

        Type DataType { get; }
    }
}
