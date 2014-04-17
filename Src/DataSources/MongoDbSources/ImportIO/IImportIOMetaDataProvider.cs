using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources.ImportIO
{
    public interface IImportIOMetaDataProvider
    {
        ImportIOInfo[] GetAllImportIOInfos(string userId);

        ImportIOInfo GetImportIOInfo(string name);

        int InsertImportIOInfo(ImportIOInfo importIOInfo, string userId);

        void UpdateImportIOInfo(ImportIOInfo importIOInfo, string userId);

        void DeleteImportIOInfo(string importIOInfoName, string userId);
    }
}
