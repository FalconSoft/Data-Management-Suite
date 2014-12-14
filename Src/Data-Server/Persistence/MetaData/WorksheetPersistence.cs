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
            Validate(wsInfo, false);
            _mongoCollections.Worksheets.Save(wsInfo);
        }

        public void Validate(WorksheetInfo wsInfo, bool failIfExists)
        {
            if (wsInfo == null)
                throw new ArgumentException("WorksheetInfo can't be null", "wsInfo");

            if (string.IsNullOrWhiteSpace(wsInfo.Urn))
                throw new ArgumentException("WorksheetInfo Urn can't be null or whitespace", "wsInfo.Urn");

            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
                throw new ArgumentException("WorksheetInfo CompanyId can't be null or whitespace", "WorksheetInfo.CompanyId");

            if (String.Compare(wsInfo.Urn, wsInfo.CreateUrn(), StringComparison.Ordinal) != 0)
                throw new ArgumentException(string.Format("WorksheetInfo Urn is incorrect. It should be {0} instead of {1}", wsInfo.CreateUrn(), wsInfo.Urn), "WorksheetInfo.CompanyId");

            if (wsInfo.Columns == null || wsInfo.Columns.Count == 0)
                throw new ArgumentException("WorksheetInfo must have columns", "WorksheetInfo.Columns");

            if (string.IsNullOrWhiteSpace(wsInfo.DataSourceInfoPath))
                throw new ArgumentException("WorksheetInfo DataSourceInfoPath can't be null or whitespace", "wsInfo.DataSourceInfoPath");

            if (!_mongoCollections.DataSources.Exists("Urn", wsInfo.DataSourceInfoPath))
            {
                throw new ArgumentException(string.Format("WorksheetInfo is trying to reference invalid DataSourceInfoPath'{0}'",
                    wsInfo.DataSourceInfoPath));
            }

            if (failIfExists)
            {
                if (_mongoCollections.Worksheets.Exists("Urn", wsInfo.Urn))
                {
                    throw new ArgumentException(string.Format("Data Source with Urn '{0}' already exists",
                        wsInfo.Urn));
                }
            }
        }

        public void Validate(AggregatedWorksheetInfo wsInfo, bool failIfExists)
        {
            if (wsInfo == null)
                throw new ArgumentException("WorksheetInfo can't be null", "wsInfo");

            if (string.IsNullOrWhiteSpace(wsInfo.Urn))
                throw new ArgumentException("WorksheetInfo Urn can't be null or whitespace", "wsInfo.Urn");

            if (string.IsNullOrWhiteSpace(wsInfo.CompanyId))
                throw new ArgumentException("WorksheetInfo CompanyId can't be null or whitespace", "WorksheetInfo.CompanyId");

            if (String.Compare(wsInfo.Urn, wsInfo.CreateUrn(), StringComparison.Ordinal) != 0)
                throw new ArgumentException(string.Format("WorksheetInfo Urn is incorrect. It should be {0} instead of {1}", wsInfo.CreateUrn(), wsInfo.Urn), "WorksheetInfo.CompanyId");

            if (wsInfo.Columns == null || wsInfo.Columns.Count == 0)
                throw new ArgumentException("WorksheetInfo must have columns", "WorksheetInfo.Columns");

            if (string.IsNullOrWhiteSpace(wsInfo.DataSourceInfoPath))
                throw new ArgumentException("WorksheetInfo Urn can't be null or whitespace", "wsInfo.DataSourceInfoPath");

            if (!_mongoCollections.DataSources.Exists("Urn", wsInfo.DataSourceInfoPath))
            {
                throw new ArgumentException(string.Format("WorksheetInfo is trying to reference invalid DataSourceInfoPath'{0}'",
                    wsInfo.DataSourceInfoPath));
            }

            if (failIfExists)
            {
                if (_mongoCollections.AggregatedWorksheets.Exists("Urn", wsInfo.Urn))
                {
                    throw new ArgumentException(string.Format("Data Source with Urn '{0}' already exists",
                        wsInfo.Urn));
                }
            }
        }

        public WorksheetInfo CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            Validate(wsInfo, true);

            var collection = _mongoCollections.Worksheets;
            wsInfo.Id = Convert.ToString(ObjectId.GenerateNewId());            
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
