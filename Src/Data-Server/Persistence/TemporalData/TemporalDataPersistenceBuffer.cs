using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.TemporalData
{
    public class TemporalDataPersistenceBuffer : ITemporalDataPersistense
    {

        private const string BsonTimeStamp = "@TimeStamp";
        private const string BsonUserId = "@UserId";
        private const string BsonRevisionId = "@RevisionId";
        private const string BsonId = "_id";
        private const string BsonLoginName = "@LoginName";

        private readonly string _dataSourceProviderString;
        private readonly string _userId;
        private readonly string[] _dbfields = { "RecordKey", BsonTimeStamp, BsonUserId, BsonId, BsonRevisionId };
        private readonly DataSourceInfo _dataSourceInfo;
        private MongoCollection<BsonDocument> _collection;
        private MongoCollection<BsonDocument> _collectionRevision; 
        //BUFFER
        private readonly int _buffer = 100 - 1;
        //ROLLOVER
        private readonly bool _rollover = false;

        private readonly LiveDataMongoCollections _mongoCollections;

        public TemporalDataPersistenceBuffer(LiveDataMongoCollections mongoCollections, DataSourceInfo dataSourceInfo, string userId, int buffer, bool rollover = false)
        {
            _mongoCollections = mongoCollections;
            _dataSourceProviderString = dataSourceInfo.Urn;
            _userId = userId;
            _dataSourceInfo = dataSourceInfo;
            _buffer = buffer - 1; 
            _rollover = rollover;
            ConnectToDb();
        }

        private void ConnectToDb()
        {
            _collection = _mongoCollections.GetHistoryDataCollection(_dataSourceProviderString);
            _collectionRevision = _mongoCollections.Revisions;

            _collectionRevision.CreateIndex(new[] { BsonId });
            _collection.CreateIndex("RecordKey", "Current");
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(string recordKey)
        {
            var cursorData = _collection.Find(Query.EQ("RecordKey", recordKey));

            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    var loginname = bsondocument[BsonUserId].ToString() == string.Empty ? _dataSourceProviderString : bsondocument[BsonUserId].ToString();
                    var dict = new Dictionary<string, object>
                    {
                        {BsonLoginName, loginname},
                        {BsonTimeStamp, bsondocument[BsonTimeStamp].ToNullableUniversalTime()}
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
            return list.OrderByDescending(h => h[BsonTimeStamp]);

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(DateTime timeStamp, string dataSourcePath)
        {
            var cursorData = _mongoCollections.GetHistoryDataCollection(dataSourcePath).FindAllAs<BsonDocument>();
            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    if (bsondocument[BsonTimeStamp].ToNullableUniversalTime() > timeStamp) continue;
                    var dict = bsondocument.ToBsonDocument().Where(data => _dbfields.All(a => a != data.Name)).ToDictionary(data => data.Name, data => BsonSerializer.Deserialize<object>(data.Value.ToJson()));
                    list.Add(dict);
                }
            }
            return list;
        }

        public IEnumerable<Dictionary<string, object>> GetDataByLTEDate(DateTime timeStamp, string dataSourcePath)
        {
            var cursorData = _mongoCollections.GetHistoryDataCollection(dataSourcePath).FindAllAs<BsonDocument>();
            var resultData = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                var timeData = cdata["Data"].AsBsonArray
                    .Where(w => w.ToString() != "{ }")
                    .Where(bson => bson[BsonTimeStamp].ToNullableUniversalTime() <= timeStamp).ToArray();
                var maxTimeStamp = timeData.Select(s => s[BsonTimeStamp].ToNullableUniversalTime()).Max();
                if (maxTimeStamp == null) continue;
                var result = timeData.First(w => w[BsonTimeStamp].ToNullableUniversalTime() == maxTimeStamp).ToBsonDocument().Where(data => _dbfields.All(a => a != data.Name))
                                                               .ToDictionary(data => data.Name, data => BsonSerializer.Deserialize<object>(data.Value.ToJson()));
                resultData.Add(result);

            }
            return resultData;
        }
        public IEnumerable<Dictionary<string, object>> GetTemporalDataByTag(TagInfo tagInfo)
        {
            var mainDataHistory = GetDataByLTEDate(tagInfo.TimeStamp, tagInfo.DataSourceProviderString);
            //var resultData = new List<Dictionary<string, object>>();
            //foreach (var relationshipInfo in _dataSourceInfo.Relationships.Values)
            //{
            //    var relatedDataHistory = GetDataByLTEDate(tagInfo.TimeStamp, relationshipInfo.RelatedSourceInfoProviderString).ToArray();
            //    if(!relatedDataHistory.Any()) continue;
            //    foreach (var mappedField in relationshipInfo.MappedFields)
            //    {
            //        mainDataHistory.Where(w => w.ContainsKey(mappedField.Key)).Join(relatedDataHistory.Where(w => w.ContainsKey(mappedField.Key)), j1 => j1[mappedField.Key].ToString(), j2 => j2[mappedField.Value].ToString(),
            //            (j1, j2) =>
            //            {
            //                foreach (var field in _dataSourceInfo.Fields.Where(w => w.Value.RelationUrn == relationshipInfo.Name))
            //                {
            //                    if (!j1.ContainsKey(field.Key) || !j2.ContainsKey(field.Value.RelatedFieldName)) continue;
            //                    j1[field.Key] = j2[field.Value.RelatedFieldName];
            //                }
            //                resultData.Add(j1);
            //                return j1;
            //            }).Count();
            //    }
            //}
            //if (!resultData.Any())
            //    return mainDataHistory;
            //return resultData.GroupBy(gr=>gr[_dataSourceInfo.GetKeyFieldsName().First()]).Select(s=>s.Last());
            return mainDataHistory.GroupBy(gr => gr[_dataSourceInfo.GetKeyFieldsName().First()]).Select(s => s.Last());
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(object revisionId)
        {
            var resultData = new List<Dictionary<string, object>>();
            var revisions = _collection.FindAll();
            foreach (var revision in revisions)
            {
                foreach (var element in revision["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    if (element[BsonRevisionId].ToString() == revisionId.ToString())
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
            var query = Query.EQ("Urn", _dataSourceInfo.Urn);
            var collectionRevision = _mongoCollections.Revisions;
            var revisions = collectionRevision.FindAs<BsonDocument>(query).SetFields(Fields.Exclude("Urn")); //"_id", "RevisionId" 
            return revisions.Select(revision => revision.ToDictionary(k => k.Name, v => (object) v.Value.ToString())).ToList();
        }

        public object AddRevision(string urn,string userId)
        {
            var revisions = _mongoCollections.Revisions;
            var revisionId = ObjectId.GenerateNewId();
            var bson = new BsonDocument
            {
                {BsonId, revisionId},
                {BsonLoginName, string.IsNullOrEmpty(userId) ? urn : userId},
                {BsonTimeStamp, DateTime.Now},
                {"Urn",urn}
            };
            revisions.Insert(bson);
            return revisionId;
        }

        public void SaveTempotalData(RecordChangedParam recordChangedParam, object revisionId)
        {
           // var revisionsId = AddRevision(recordChangedParam.ProviderString, recordChangedParam.UserToken);
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
                CreateNewDoucument(_collection, recordChangedParam, (ObjectId)revisionId);
            else
            {
                switch (recordChangedParam.ChangedAction)
                {
                    case RecordChangedAction.AddedOrUpdated:
                    {
                            var bsDoc = GetDataForHistory(recordChangedParam);
                            bsDoc = MergeHistory(bsDoc,cursor);
                            AddStructureFields(ref bsDoc, recordChangedParam, (ObjectId)revisionId);
                            var query = Query.EQ("RecordKey", recordChangedParam.RecordKey);
                            //if current == buffer then create new doc
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == true)//ROLLOVER ON
                            {
                                var doc = cursor["Data"].AsBsonArray.Last();
                                doc[BsonTimeStamp] = DateTime.Now;
                                _collection.Update(query, Update.Set(string.Format("Data.{0}", cursor["Current"].AsInt32), doc));
                                CreateNewDoucument(_collection, recordChangedParam, (ObjectId)revisionId);
                                break;
                            }
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == false)//ROLLOVER OFF
                            {
                                RemoveRevision(query, 0, _collection, true);
                               _collection.Update(query, Update.Set(string.Format("Data.{0}", 0), bsDoc.ToBsonDocument()).Set("Current", 0).Set("OverBuffer",true));
                                break;
                            }
                            var currentNum = cursor["Current"].AsInt32 + 1;
                            RemoveRevision(query, currentNum, _collection, cursor["OverBuffer"].AsBoolean);
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

        public void UpdateTemporalData(RecordChangedParam recordChangedParam)
        {
            var cursor = _collection.FindOne(Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey), Query.LTE("Current", _buffer)));
            if (cursor == null) return;
            var index = cursor["Current"].AsInt32;
            var updateQ = new  UpdateBuilder();
            foreach (var record in recordChangedParam.RecordValues)
                updateQ.Set(string.Format("Data.{0}.{1}",index, record.Key),record.Value == null? BsonNull.Value : BsonValue.Create(record.Value));
            var queryq = Query.EQ("RecordKey", recordChangedParam.RecordKey);
            _collection.Update(queryq, updateQ);
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            var collection = _mongoCollections.TagInfos;
            collection.Insert(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var collection = _mongoCollections.TagInfos;
            var query = Query<TagInfo>.EQ(t => t.TagName, tagInfo.TagName);
            collection.Remove(query);
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            var collection = _mongoCollections.TagInfos;
            return collection.FindAll().SetFields(Fields.Exclude(BsonId)).ToList();
        }

        private void CreateNewDoucument(MongoCollection<BsonDocument> collection, RecordChangedParam recordChangedParam, ObjectId revisionId)
        {
            var bsDoc = new TempDataObject
            {
                RecordKey = recordChangedParam.RecordKey,
                Current = 0,
                Total = _buffer + 1,
                OverBuffer = false
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


        void AddStructureFields(ref Dictionary<string, object> bsonDocument, RecordChangedParam recordChangedParam, ObjectId revisionId)
        {
            bsonDocument.Add(BsonRevisionId, revisionId);
            bsonDocument.Add(BsonId, ObjectId.GenerateNewId());
            bsonDocument.Add(BsonTimeStamp, DateTime.Now);
            bsonDocument.Add(BsonUserId, string.IsNullOrEmpty(recordChangedParam.UserToken) ? string.Empty : recordChangedParam.UserToken);
        }

        private Dictionary<string, object> GetDataForHistory(RecordChangedParam recordChangedParam)
        {
            //if(recordChangedParam.ChangedPropertyNames == null)
            //    return new Dictionary<string, object>(recordChangedParam.RecordValues);
            //return recordChangedParam.ChangedPropertyNames.ToDictionary(changedPropertyName => changedPropertyName, changedPropertyName => recordChangedParam.RecordValues[changedPropertyName]);
            return new Dictionary<string, object>(recordChangedParam.RecordValues);
        }

        private Dictionary<string, object> MergeHistory(Dictionary<string, object> dataForMerge, BsonDocument cursor)
        {
           var historyData = cursor["Data"].AsBsonArray[cursor["Current"].AsInt32].AsBsonDocument.ToDictionary(k => k.Name, v => v.Value);
           var prevData = historyData.Where(bsonValue => _dbfields.All(a => a != bsonValue.Key)).ToDictionary(bsonValue => bsonValue.Key, bsonValue => ToStrongTypedObject(bsonValue.Value, bsonValue.Key));
            foreach (var record in dataForMerge)
            {
                if (prevData.ContainsKey(record.Key))
                    prevData[record.Key] = record.Value;
                else prevData.Add(record.Key,record.Value);
            }
            return prevData;
        }


        private void RemoveRevision(IMongoQuery query,int index,MongoCollection<BsonDocument> historyCollection, bool overBuffer)
        {
            if (overBuffer == false) return;
            var obj = historyCollection.FindAs<BsonDocument>(query);
            if (obj == null) return;
            try
            {
                var revisionId = obj.First()["Data"].AsBsonArray[index][BsonRevisionId].AsObjectId;
                var queryRev = Query.EQ(BsonId, revisionId);
                _collectionRevision.Remove(queryRev);
            }
            catch (Exception)
            {}
           
        }


        private object ToStrongTypedObject(BsonValue bsonValue, string fieldName)
        {
            if (fieldName == "RecordKey") return bsonValue.ToString();
            if(!_dataSourceInfo.Fields.ContainsKey(fieldName)) return null;
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

        public bool OverBuffer { get; set; }

        public Dictionary<string, object>[] Data { get; set; }
    }
}
