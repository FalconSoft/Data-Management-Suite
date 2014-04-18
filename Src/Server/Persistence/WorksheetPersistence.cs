using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence
{
    public class WorksheetPersistence : IWorksheetPersistence
    {
        private readonly string _connectionString;
        
        private const string WorksheetInfoCollectionName = "WorksheetInfo";

        private const string AggregatedWorksheetInfoCollectionName = "AggregatedWorksheetInfo";

        public WorksheetPersistence(string connectionString)
        {
            _connectionString = connectionString;
        }

        public WorksheetInfo GetWorksheetInfo(string urn)
        {
            var db = MongoDatabase.Create(_connectionString);
            return db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", urn.GetName()),
                                                Query.EQ("Category", urn.GetCategory())));
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var db = MongoDatabase.Create(_connectionString);
            return db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName).FindAll().ToArray();
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            var collection = db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName);
            collection.Save(wsInfo);
        }

        public WorksheetInfo CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            var collection = db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName);
            wsInfo.Id = ObjectId.GenerateNewId().ToString();
            collection.Insert(wsInfo);
            return collection.FindOneAs<WorksheetInfo>(Query.And(Query.EQ("Name", wsInfo.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", wsInfo.DataSourcePath.GetCategory())));
        }

        public void DeleteWorksheetInfo(string worksheetId, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            db.GetCollection(WorksheetInfoCollectionName).Remove(Query.EQ("Id", worksheetId));
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn)
        {
            var db = MongoDatabase.Create(_connectionString);
            return db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                Query.EQ("Category", worksheetUrn.GetCategory())));
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var db = MongoDatabase.Create(_connectionString);
            return db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName).FindAll().ToArray();
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            var collection = db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName);
            collection.Save(wsInfo);
        }

        public AggregatedWorksheetInfo CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            var collection = db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName);
            wsInfo.Id = ObjectId.GenerateNewId().ToString();
            collection.Insert(wsInfo);
            return collection.FindOneAs<AggregatedWorksheetInfo>(Query.And(Query.EQ("Name", wsInfo.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", wsInfo.DataSourcePath.GetCategory())));
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            var db = MongoDatabase.Create(_connectionString);
            db.GetCollection(AggregatedWorksheetInfoCollectionName).Remove(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                                  Query.EQ("Category", worksheetUrn.GetCategory())));
        }
    }
}
