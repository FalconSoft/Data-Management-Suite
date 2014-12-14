using System;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.MetaData
{
    public class WorksheetPersistence : IWorksheetPersistence
    {
        private readonly MetaDataMongoCollections _mongoCollections;
       
        public WorksheetPersistence(MetaDataMongoCollections mongoCollections)
        {
            _mongoCollections = mongoCollections;
        }

        public WorksheetInfo GetWorksheetInfo(string urn, string userId)
        {
            return _mongoCollections.Worksheets.FindOne(Query.EQ("Urn", urn));
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var companyId = _mongoCollections.GetCompanyId(userId);
            return _mongoCollections.Worksheets.FindAs<WorksheetInfo>(Query<WorksheetInfo>.EQ(d => d.CompanyId, companyId)).ToArray();
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
            {
                wsInfo.CompanyId = _mongoCollections.GetCompanyId(userId);
            }

            _mongoCollections.Worksheets.Save(wsInfo);
        }

        public WorksheetInfo CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            var collection = _mongoCollections.Worksheets;
            wsInfo.Id = Convert.ToString(ObjectId.GenerateNewId());

            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
            {
                wsInfo.CompanyId = _mongoCollections.GetCompanyId(userId);
            }
            
            collection.Insert(wsInfo);

            return collection.FindOneAs<WorksheetInfo>(Query.EQ("Urn", wsInfo.Urn));
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            _mongoCollections.Worksheets.Remove(Query.EQ("Urn", worksheetUrn));
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            return _mongoCollections.AggregatedWorksheets.FindOne(Query.EQ("Urn", worksheetUrn));
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var companyId = _mongoCollections.GetCompanyId(userId);
            return _mongoCollections.AggregatedWorksheets.FindAs<AggregatedWorksheetInfo>(Query<AggregatedWorksheetInfo>.EQ(d => d.CompanyId, companyId)).ToArray();
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
            {
                wsInfo.CompanyId = _mongoCollections.GetCompanyId(userId);
            }

            _mongoCollections.AggregatedWorksheets.Save(wsInfo);
        }

        public AggregatedWorksheetInfo CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            var collection = _mongoCollections.AggregatedWorksheets;
            wsInfo.Id = Convert.ToString(ObjectId.GenerateNewId());

            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
            {
                wsInfo.CompanyId = _mongoCollections.GetCompanyId(userId);
            }
            
            collection.Insert(wsInfo);
            return collection.FindOneAs<AggregatedWorksheetInfo>(Query.EQ("Urn", wsInfo.Urn)); ;
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            _mongoCollections.AggregatedWorksheets.Remove(Query.EQ("Urn", worksheetUrn));
        }
    }
}
