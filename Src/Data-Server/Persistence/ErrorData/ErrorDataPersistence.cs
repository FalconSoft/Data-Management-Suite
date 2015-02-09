using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.ErrorData
{
    public class ErrorDataPersistence : IErrorDataPersistence
    {
        private readonly LiveDataMongoCollections _liveDataCollections;

        public ErrorDataPersistence(LiveDataMongoCollections liveDataCollections)
        {
            _liveDataCollections = liveDataCollections;
        }

        public void SaveErrorData(string urn, ErrorDataObject errorData)
        {
           _liveDataCollections.GetErrorDataCollection(urn).Insert(errorData);
        }
    }
}
