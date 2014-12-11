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

        private string GetCategoryPart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').First();
        }

        private string GetNamePart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').Last();
        }
        
        public WorksheetPersistence(MetaDataMongoCollections mongoCollections)
        {
            _mongoCollections = mongoCollections;
        }

        public WorksheetInfo GetWorksheetInfo(string urn, string userId)
        {
            var companyId = _mongoCollections.GetCompanyId(userId);
            return _mongoCollections.Worksheets
                    .FindOne(Query.And(
                                Query.EQ("Name", GetNamePart(urn)),
                                Query.EQ("CompanyId", companyId),
                                Query.EQ("Category", GetCategoryPart(urn))));
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var companyId = _mongoCollections.GetCompanyId(userId);
            return _mongoCollections.DataSources.FindAs<WorksheetInfo>(Query<WorksheetInfo>.EQ(d => d.CompanyId, companyId)).ToArray();
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
            return collection.FindOneAs<WorksheetInfo>(Query.And(Query.EQ("Name", GetNamePart(wsInfo.DataSourcePath)),
                                                                  Query.EQ("Category", GetCategoryPart(wsInfo.DataSourcePath))));
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            _mongoCollections.Worksheets.Remove(Query.And(Query.EQ("Name", GetNamePart(worksheetUrn)),
                                                                  Query.EQ("Category", GetCategoryPart(worksheetUrn))));
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            return _mongoCollections.AggregatedWorksheets
                    .FindOne(Query.And(Query.EQ("Name", GetNamePart(worksheetUrn)),
                                                Query.EQ("Category", GetCategoryPart(worksheetUrn))));
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var companyId = _mongoCollections.GetCompanyId(userId);
            return _mongoCollections.DataSources.FindAs<AggregatedWorksheetInfo>(Query<AggregatedWorksheetInfo>.EQ(d => d.CompanyId, companyId)).ToArray();
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
            return collection.FindOneAs<AggregatedWorksheetInfo>(Query.And(Query.EQ("Name", GetNamePart(wsInfo.DataSourcePath)),
                                                                  Query.EQ("Category",  GetCategoryPart(wsInfo.DataSourcePath))));
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            _mongoCollections.AggregatedWorksheets.Remove(Query.And(Query.EQ("Name", GetNamePart(worksheetUrn)),
                                                                  Query.EQ("Category", GetCategoryPart(worksheetUrn))));
        }
    }
}
