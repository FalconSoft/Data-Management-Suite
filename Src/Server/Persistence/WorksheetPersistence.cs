using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Facade;
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
            var ws = db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", urn.GetName()),
                                                Query.EQ("Category", urn.GetCategory())));
            var columns = ws.Columns.Join(ws.DataSourceInfo.Fields, j1 => j1.FieldName, j2 => j2.Key,
                  (j1, j2) =>
                  {
                      j1.Update(j2.Value);
                      return j1;
                  }).ToList();
            var removedColumns = columns.Select(s => s.FieldName).Except(ws.DataSourceInfo.Fields.Keys).ToList();
            foreach (var removedColumn in removedColumns)
            {
                columns.Remove(columns.First(f => f.FieldName == removedColumn));
            }
            ws.Columns = columns;
            return ws;
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var db = MongoDatabase.Create(_connectionString);
            var wsCollection = db.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName).FindAll();
            foreach (var ws in wsCollection)
            {
                var columns = ws.Columns.Join(ws.DataSourceInfo.Fields, j1 => j1.FieldName, j2 => j2.Key,
                  (j1, j2) =>
                  {
                      j1.Update(j2.Value);
                      return j1;
                  }).ToList();
                var removedColumns = columns.Select(s => s.FieldName).Except(ws.DataSourceInfo.Fields.Keys).ToList();
                foreach (var removedColumn in removedColumns)
                {
                    columns.Remove(columns.First(f => f.FieldName == removedColumn));
                }
                ws.Columns = columns;
            }
            return wsCollection.ToArray();
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
            var awsi =  db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                Query.EQ("Category", worksheetUrn.GetCategory())));
            var colList = new List<ColumnInfo>();
            foreach (var col in awsi.GroupByColumns)
            {
                col.Update(awsi.DataSourceInfo.Fields[col.FieldName]);
                colList.Add(col);
            }
            awsi.GroupByColumns = colList;
            var col2List = new List<KeyValuePair<AggregatedFunction, ColumnInfo>>();
            foreach (var col in awsi.Columns)
            {
                col.Value.Update(awsi.DataSourceInfo.Fields[col.Value.FieldName]);
                col2List.Add(col);
            }
            awsi.Columns = col2List;
            return awsi;
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var db = MongoDatabase.Create(_connectionString);
            var awsCollection = db.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName).FindAll();
            foreach (var awsi in awsCollection)
            {
                var colList = new List<ColumnInfo>();
                foreach (var col in awsi.GroupByColumns)
                {
                    col.Update(awsi.DataSourceInfo.Fields[col.FieldName]);
                    colList.Add(col);
                }
                awsi.GroupByColumns = colList;
                var col2List = new List<KeyValuePair<AggregatedFunction, ColumnInfo>>();
                foreach (var col in awsi.Columns)
                {
                    col.Value.Update(awsi.DataSourceInfo.Fields[col.Value.FieldName]);
                    col2List.Add(col);
                }
                awsi.Columns = col2List;
            }
            return awsCollection.ToArray();
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
