using System;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources.ImportIO
{
    class ImportIOMetaDataProvider : IImportIOMetaDataProvider
    {
        public ImportIOInfo[] GetAllImportIOInfos(string userId)
        {
            throw new NotImplementedException();
        }

        public ImportIOInfo GetImportIOInfo(string name)
        {
            throw new NotImplementedException();
        }

        public int InsertImportIOInfo(ImportIOInfo importIOInfo, string userId)
        {
            throw new NotImplementedException();
        }

        public void UpdateImportIOInfo(ImportIOInfo importIOInfo, string userId)
        {
            throw new NotImplementedException();
        }

        public void DeleteImportIOInfo(string importIOInfoName, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
