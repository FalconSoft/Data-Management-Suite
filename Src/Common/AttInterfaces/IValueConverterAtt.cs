using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.AttInterfaces
{
    public interface IValueConverterAttribute
    {
        string Name { get; }

        DataTypes ToDataType { get; }
    }
}
