using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.TemporalData
{
    public class TemporalDataPersistenceBuffer : ITemporalDataPersistense
    {
        private readonly string _connectionString;
        private readonly string _dataSourceProviderString;
        private readonly string _userId;
        private readonly string[] _dbfields = { "RecordKey", "TimeStamp", "UserId", "_id", "RevisionId" };
        private readonly DataSourceInfo _dataSourceInfo;
        private MongoDatabase _mongoDatabase;
        private MongoCollection<BsonDocument> _collection;
        //BUFFER
        private readonly int _buffer = 100 - 1;
        //ROLLOVER
        private readonly bool _rollover = false;

        public TemporalDataPersistenceBuffer(string connectionString, DataSourceInfo dataSourceInfo, string userId, int buffer, bool rollover = false)
        {
            _connectionString = connectionString;
            _dataSourceProviderString = dataSourceInfo.DataSourcePath;
            _userId = userId;
            _dataSourceInfo = dataSourceInfo;
            _buffer = buffer - 1;
            _rollover = rollover;
            ConnectToDb();
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_connectionString);
            }
            if (_mongoDatabase.CollectionExists(_dataSourceProviderString.ToValidDbString() + "_History"))
            {
                _collection = _mongoDatabase.GetCollection(_dataSourceProviderString.ToValidDbString() + "_History");
            }
            else
            {
                _mongoDatabase.CreateCollection(_dataSourceProviderString.ToValidDbString() + "_History");
                _collection = _mongoDatabase.GetCollection(_dataSourceProviderString.ToValidDbString() + "_History");
                _collection.EnsureIndex("RecordKey");
                _collection.EnsureIndex("Current");
            }
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(string recordKey)
        {
            var cursorData = _collection.Find(Query.EQ("RecordKey", recordKey));

            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    var loginname = bsondocument["UserId"].ToString() == string.Empty ? _dataSourceProviderString : bsondocument["UserId"].ToString();
                    var dict = new Dictionary<string, object>
                    {
                        {"LoginName", loginname},
                        {"TimeStamp", bsondocument["TimeStamp"].ToNullableUniversalTime()}
                    };
                    foreach (var data in bsondocument.ToBsonDocument())
                    {
                        if (_dbfields.All(a => a != data.Name))
                        {
                            if (!_dataSourceInfo.Fields.ContainsKey(data.Name)) continue;
                            dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                        }
                    }
                    list.Add(dict);
                }
            }
            return list.OrderByDescending(h => h["TimeStamp"]);

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(DateTime timeStamp, string dataSourcePath)
        {
            var cursorData = _mongoDatabase.GetCollection(dataSourcePath.ToValidDbString() + "_History").FindAllAs<BsonDocument>();
            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    if (bsondocument["TimeStamp"].ToNullableUniversalTime() > timeStamp) continue;
                    var dict = bsondocument.ToBsonDocument().Where(data => _dbfields.All(a => a != data.Name)).ToDictionary(data => data.Name, data => BsonSerializer.Deserialize<object>(data.Value.ToJson()));
                    list.Add(dict);
                }
            }
            return list;
        }

        public IEnumerable<Dictionary<string, object>> GetDataByLTEDate(DateTime timeStamp, string dataSourcePath)
        {
            var cursorData = _mongoDatabase.GetCollection(dataSourcePath.ToValidDbString() + "_History").FindAllAs<BsonDocument>();
            var resultData = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                var timeData = cdata["Data"].AsBsonArray
                    .Where(w => w.ToString() != "{ }")
                    .Where(bson => bson["TimeStamp"].ToNullableUniversalTime() <= timeStamp).ToArray();
                var maxTimeStamp = timeData.Select(s => s["TimeStamp"].ToNullableUniversalTime()).Max();
                if (maxTimeStamp == null) continue;
                var result = timeData.First(w => w["TimeStamp"].ToNullableUniversalTime() == maxTimeStamp).ToBsonDocument().Where(data => _dbfields.All(a => a != data.Name))
                                                               .ToDictionary(data => data.Name, data => BsonSerializer.Deserialize<object>(data.Value.ToJson()));
                resultData.Add(result);

            }
            return resultData;
        }
        public IEnumerable<Dictionary<string, object>> GetTemporalDataByTag(TagInfo tagInfo)
        {
            var mainDataHistory = GetDataByLTEDate(tagInfo.TimeStamp, tagInfo.DataSourceProviderString).ToArray();
            var resultData = new List<Dictionary<string, object>>();
            foreach (var relationshipInfo in _dataSourceInfo.Relationships.Values)
            {
                var relatedDataHistory = GetDataByLTEDate(tagInfo.TimeStamp, relationshipInfo.RelatedSourceInfoProviderString).ToArray();
                if(!relatedDataHistory.Any()) continue;
                foreach (var mappedField in relationshipInfo.MappedFields)
                {
                    mainDataHistory.Join(relatedDataHistory, j1 => j1[mappedField.Key].ToString(), j2 => j2[mappedField.Value].ToString(),
                        (j1, j2) =>
                        {
                            foreach (var field in _dataSourceInfo.Fields.Where(w => w.Value.RelationUrn == relationshipInfo.Name))
                            {
                                if (!j1.ContainsKey(field.Key) || !j2.ContainsKey(field.Value.RelatedFieldName)) continue;
                                j1[field.Key] = j2[field.Value.RelatedFieldName];
                            }
                            resultData.Add(j1);
                            return j1;
                        }).Count();
                }
            }
            if (!resultData.Any())
                return mainDataHistory;
            return resultData.GroupBy(gr=>gr[_dataSourceInfo.GetKeyFieldsName().First()]).Select(s=>s.Last());
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(object revisionId)
        {
            var resultData = new List<Dictionary<string, object>>();
            var revisions = _collection.FindAll();
            foreach (var revision in revisions)
            {
                foreach (var element in revision["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    if (element["RevisionId"].ToString() == revisionId.ToString())
                    {
                        var dict = element.ToBsonDocument().Where(data => _dbfields.All(a => a != data.Name)).Where(data => _dataSourceInfo.Fields.ContainsKey(data.Name)).ToDictionary(data => data.Name, data => BsonSerializer.Deserialize<object>(data.Value.ToJson()));
                        resultData.Add(dict);
                    }
                }  
            }

            return resultData;
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions()
        {
            var query = Query.EQ("Urn", _dataSourceInfo.DataSourcePath);
            var collectionRevision = _mongoDatabase.GetCollection("Revisions");
            var revisions = collectionRevision.FindAs<BsonDocument>(query).SetFields(Fields.Exclude("_id", "Urn")); //, "RevisionId"
            return revisions.Select(revision => revision.ToDictionary(k => k.Name, v => (object) v.Value.ToString())).ToList();
        }

        public Guid AddRevision(string urn,string userId)
        {
            var revisions = _mongoDatabase.GetCollection("Revisions");
            var revisionId = Guid.NewGuid();
            var bson = new BsonDocument
            {
                {"RevisionId", revisionId},
                {"LoginName", string.IsNullOrEmpty(userId) ? urn : userId},
                {"TimeStamp", DateTime.Now},
                {"Urn",urn}
            };
            revisions.Insert(bson);
            return revisionId;
        }

        public void SaveTempotalData(RecordChangedParam recordChangedParam, object revisionId)
        {
            if(recordChangedParam.ChangedAction == RecordChangedAction.Removed) return;
            if (!string.IsNullOrEmpty(recordChangedParam.OriginalRecordKey) &&
                (recordChangedParam.OriginalRecordKey != recordChangedParam.RecordKey))
            {
                var query = Query.EQ("RecordKey", recordChangedParam.OriginalRecordKey);
                var update = Update.Set("RecordKey", recordChangedParam.RecordKey);
                _collection.Update(query, update, UpdateFlags.Multi);
            }
            var cursor = _collection.FindOne(Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey), Query.LTE("Current", _buffer)));
            if (cursor == null)
                CreateNewDoucument(_collection, recordChangedParam, (Guid)revisionId);
            else
            {
                switch (recordChangedParam.ChangedAction)
                {
                    case RecordChangedAction.AddedOrUpdated:
                        {
                            var bsDoc = new Dictionary<string, object>(recordChangedParam.RecordValues);
                            AddStructureFields(ref bsDoc, recordChangedParam, (Guid)revisionId);
                            var query = Query.EQ("RecordKey", recordChangedParam.RecordKey);
                            //if current == buffer then create new doc
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == true)//ROLLOVER ON
                            {
                                var doc = cursor["Data"].AsBsonArray.Last();
                                doc["TimeStamp"] = DateTime.Now;
                                _collection.Update(query, Update.Set(string.Format("Data.{0}", cursor["Current"].AsInt32), doc));
                                CreateNewDoucument(_collection, recordChangedParam, (Guid)revisionId);
                                break;
                            }
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == false)//ROLLOVER OFF
                            {
                                _collection.Update(query, Update.Set(string.Format("Data.{0}", _buffer), bsDoc.ToBsonDocument()).Set("Current", 0));
                                break;
                            }
                            var currentNum = cursor["Current"].AsInt32 + 1;
                            var update = Update.Set(string.Format("Data.{0}", currentNum), bsDoc.ToBsonDocument()).Set("Current", currentNum);
                            _collection.Update(query, update);
                            break;
                        }
                    case RecordChangedAction.Removed:
                        {
                            //var query = Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey));
                            //var index = collection.Find(query).First()["Current"].ToString();
                            //collection.Update(query, Update.Set("Data." + index + ".TimeStamp", DateTime.Now), UpdateFlags.Multi);
                        }
                        break;
                }
            }
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            collection.Insert(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            var query = Query<TagInfo>.EQ(t => t.TagName, tagInfo.TagName);
            collection.Remove(query);
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            return collection.FindAll().SetFields(Fields.Exclude("_id")).ToList();
        }

        private void CreateNewDoucument(MongoCollection<BsonDocument> collection, RecordChangedParam recordChangedParam, Guid revisionId)
        {
            var bsDoc = new TempDataObject
            {
                RecordKey = recordChangedParam.RecordKey,
                Current = 0,
                Total = _buffer + 1
            };
            var bsItem = new Dictionary<string, object>(recordChangedParam.RecordValues);
            AddStructureFields(ref bsItem, recordChangedParam, revisionId);
            var array = new List<Dictionary<string, object>>();
            var bsonStructure = new Dictionary<string, object>();// { { "TimeStamp", null }, { "UserId", null } };
            array.Add(bsItem);
            for (var i = 1; i < _buffer; i++)
                array.Add(bsonStructure);
            bsDoc.Data = array.ToArray();
            collection.Insert(bsDoc);
        }


        void AddStructureFields(ref Dictionary<string, object> bsonDocument, RecordChangedParam recordChangedParam, Guid revisionId)
        {
            bsonDocument.Add("RevisionId", revisionId);
            bsonDocument.Add("_id", ObjectId.GenerateNewId());
            bsonDocument.Add("TimeStamp", DateTime.Now);
            bsonDocument.Add("UserId", string.IsNullOrEmpty(recordChangedParam.UserToken) ? string.Empty : recordChangedParam.UserToken);
        }



        private object ToStrongTypedObject(BsonValue bsonValue, string fieldName)
        {
            if (fieldName == "RecordKey") return bsonValue.ToString();
            var dataType = _dataSourceInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            if (string.IsNullOrEmpty(bsonValue.ToString()) || bsonValue.ToString() == "BsonNull") return null;
            switch (dataType)
            {
                case DataTypes.Int:
                    return bsonValue.ToInt32();
                case DataTypes.Double:
                    return bsonValue.ToDouble();
                case DataTypes.String:
                    return bsonValue.ToString();
                case DataTypes.Bool:
                    return bsonValue.ToBoolean();
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return Convert.ToDateTime(bsonValue.ToString());
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }
    }

    public class TempDataObject
    {
        public string RecordKey { get; set; }

        public int Current { get; set; }

        public int Total { get; set; }

        public Dictionary<string, object>[] Data { get; set; }
    }
}
