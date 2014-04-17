namespace FalconSoft.ReactiveWorksheets.Common.AttInterfaces
{
    public interface IImportDataInfoAttribute
    {
        string Category { get; }

        string Name { get; }

        string Uri { get; }

        string[] Fields { get; }
    }
}
