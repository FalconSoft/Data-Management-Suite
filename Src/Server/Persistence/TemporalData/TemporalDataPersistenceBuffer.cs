using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence.TemporalData
{
    public class TemporalDataPersistenceBuffer : ITemporalDataPersistense
    {
        private readonly string _connectionString;
        private readonly string _dataSourceProviderString;
        private readonly string _userId;
        private readonly string[] _dbfields = { "RecordKey", "TimeStamp", "UserId", "_id" };
        private readonly DataSourceInfo _dataSourceInfo;
        private MongoDatabase _mongoDatabase;

        //BUFFER
        private readonly int _buffer = 100 - 1;
        //ROLLOVER
        private readonly bool _rollover = false;

        public TemporalDataPersistenceBuffer(string connectionString, DataSourceInfo dataSourceInfo, string userId, int buffer,bool rollover = false)
        {
            _connectionString = connectionString;
            _dataSourceProviderString = dataSourceInfo.DataSourcePath;
            _userId = userId;
            _dataSourceInfo = dataSourceInfo;
            _buffer = buffer - 1;
            _rollover = rollover;
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_connectionString);
            }
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(string recordKey)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<BsonDocument>(_dataSourceProviderString.ToValidDbString() + "_History");
            var users = _mongoDatabase.GetCollection<BsonDocument>("Users");
            var cursorData = collection.Find(Query.EQ("RecordKey", recordKey));
            var cursorUser = users.FindAllAs<BsonDocument>();

            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    var user = cursorUser.FirstOrDefault(f => f["_id"].ToString() == bsondocument["UserId"].ToString());
                    var loginname = user == null ? _dataSourceProviderString : user["LoginName"].ToString();
                    var dict = new Dictionary<string, object>
                    {
                        {"LoginName", loginname},
                        {"TimeStamp", bsondocument["TimeStamp"].ToNullableUniversalTime()}
                    };
                    foreach (var data in bsondocument.ToBsonDocument())
                    {
                        if (_dbfields.All(a => a != data.Name))
                            dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                    }
                    list.Add(dict);
                }
            }
            return list.OrderByDescending(h => h["TimeStamp"]);

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(DateTime timeStamp)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<BsonDocument>(_dataSourceProviderString.ToValidDbString() + "_History");
            var users = _mongoDatabase.GetCollection<BsonDocument>("Users");
            var cursorData = collection.FindAllAs<BsonDocument>();
            var cursorUser = users.FindAllAs<BsonDocument>();
            var list = new List<Dictionary<string, object>>();

            foreach (var cdata in cursorData)
            {
                foreach (var bsondocument in cdata["Data"].AsBsonArray.Where(w => w.ToString() != "{ }"))
                {
                    if (bsondocument["TimeStamp"].ToNullableUniversalTime() > timeStamp) continue;
                    var user = cursorUser.FirstOrDefault(f => f["_id"].ToString() == bsondocument["UserId"].ToString());
                    var loginname = user == null ? _dataSourceProviderString : user["LoginName"].ToString();
                    var dict = new Dictionary<string, object>
                    {
                        {"LoginName", loginname},
                        {"TimeStamp", bsondocument["TimeStamp"].ToNullableUniversalTime()}
                    };
                    foreach (var data in bsondocument.ToBsonDocument())
                    {
                        if (_dbfields.All(a => a != data.Name))
                            dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                    }
                    list.Add(dict);
                }
            }
            return list;
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByTag(TagInfo tagInfo)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<BsonDocument>(tagInfo.DataSourceProviderString.ToValidDbString() + "_History");
            var cursorData = collection.FindAllAs<BsonDocument>();
            string[] exceptfields = { "TimeStamp", "UserId", "_id" };
            var list = new List<Dictionary<string, object>>();

            foreach (var cursor in cursorData)
            {
                cursor["Data"].AsBsonArray.Where(w => w.ToString() != "{ }").Join(tagInfo.Revisions, j1 => j1["_id"].ToString(), j2 => j2, (j1, j2) =>
                {
                    var dict = new Dictionary<string, object> { { "RecordKey", cursor["RecordKey"].ToString() } };
                    foreach (var data in j1.ToBsonDocument())
                    {
                        if (exceptfields.All(a => a != data.Name))
                            dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                    }
                    list.Add(dict);
                    return j2;
                }).Count();
            }
            return list;
        }

        public void SaveTempotalData(RecordChangedParam recordChangedParam)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<BsonDocument>(recordChangedParam.ProviderString.ToValidDbString() + "_History");
            if (!string.IsNullOrEmpty(recordChangedParam.OriginalRecordKey) &&
                (recordChangedParam.OriginalRecordKey != recordChangedParam.RecordKey))
            {
                var query = Query.EQ("RecordKey", recordChangedParam.OriginalRecordKey);
                var update = Update.Set("RecordKey", recordChangedParam.RecordKey);
                collection.Update(query, update, UpdateFlags.Multi);
            }
            var cursor = collection.FindOne(Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey), Query.LTE("Current", _buffer))); 
            if (cursor == null)
            {
                CreateNewDoucument(collection, recordChangedParam);
            }
            else
            {
                switch (recordChangedParam.ChangedAction)
                {
                    case RecordChangedAction.AddedOrUpdated:
                        {
                            var bsDoc = new BsonDocument();
                            AddStructureFields(ref bsDoc, recordChangedParam.UserToken);
                            bsDoc.AddRange(recordChangedParam.RecordValues.ToArray());
                            var query = Query.EQ("_id", cursor["_id"]);
                            //if current == buffer then create new doc
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == true)//ROLLOVER ON
                            {
                                var doc = cursor["Data"].AsBsonArray.Last();
                                doc["TimeStamp"] = DateTime.Now;
                                collection.Update(query, Update.Set(string.Format("Data.{0}", cursor["Current"].AsInt32), doc));
                                CreateNewDoucument(collection, recordChangedParam);
                                break;
                            }
                            if (cursor["Current"].AsInt32 == _buffer && _rollover == false)//ROLLOVER OFF
                            {
                                collection.Update(query, Update.Set(string.Format("Data.{0}", _buffer), bsDoc).Set("Current",0));
                                break;
                            }
                            var num = cursor["Current"].AsInt32 + 1;
                            var update = Update.Set(string.Format("Data.{0}", num), bsDoc).Set("Current", num);
                            collection.Update(query, update);
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
            ConnectToDb();
            var recordsHistory = _mongoDatabase.GetCollection<BsonDocument>(tagInfo.DataSourceProviderString.ToValidDbString() + "_History");
            tagInfo.Revisions =
                recordsHistory.FindAll()
                    .Select(s => s["Data"].AsBsonArray[s["Current"].AsInt32])
                    .Select(s => s.ToBsonDocument()["_id"].ToString()).ToList();
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            collection.Insert(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            var query = Query<TagInfo>.EQ(t => t.TagName, tagInfo.TagName);
            collection.Remove(query);
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<TagInfo>("TagInfo");
            return collection.FindAll().SetFields(Fields.Exclude("_id")).ToList();
        }

        private void CreateNewDoucument(MongoCollection<BsonDocument> collection, RecordChangedParam recordChangedParam)
        {
            var bsDoc = new BsonDocument
                {
                    {"RecordKey", recordChangedParam.RecordKey},
                    {"Current", 0},
                    {"Total", _buffer + 1}
                };
            var bsItem = new BsonDocument();
            AddStructureFields(ref bsItem, recordChangedParam.UserToken);
            bsItem.AddRange(recordChangedParam.RecordValues);
            var array = new BsonArray();
            var bsonStructure = new BsonDocument { { "TimeStamp", null }, { "UserId", null } };
            array.Add(bsItem);
            for (var i = 1; i < _buffer; i++)
                array.Add(bsonStructure);
            bsDoc.Add("Data", array);
            collection.Insert(bsDoc);
        }


        void AddStructureFields(ref BsonDocument bsonDocument, string userToken)
        {
            bsonDocument.Add("_id", ObjectId.GenerateNewId());
            bsonDocument.Add("TimeStamp", DateTime.Now);
            bsonDocument.Add("UserId", string.IsNullOrEmpty(userToken) ? string.Empty : userToken);
        }



        private object ToStrongTypedObject(BsonValue bsonValue, string fieldName)
        {
            if (fieldName == "RecordKey") return bsonValue.ToString();
            var dataType = _dataSourceInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            if (string.IsNullOrEmpty(bsonValue.ToString())) return null;
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
                    return bsonValue.ToNullableUniversalTime();
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }
    }
}
