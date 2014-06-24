using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.ErrorData
{
    public class ErrorDataPersistence:IErrorDataPersistence
    {
        private readonly MongoDatabase _db;

        public ErrorDataPersistence(string connectionString)
        {
            _db = MongoDatabase.Create(connectionString);
        }

        public void SaveErrorData(string urn, ErrorDataObject errorData)
        {
           var collection = _db.GetCollection<ErrorDataObject>(urn.ToValidDbString() + "_ErrorData");
           collection.Insert(errorData);
        }
    }
}
