using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence.TemporalData
{
    public class TemporalDataPersistenceTime : ITemporalDataPersistense
    {
         private readonly string _connectionString;
        private readonly string _dbName;
        private readonly string _dataSourceProviderString;
        private readonly string _userId;
        private readonly string[] _dbfields = { "RecordKey", "TimeStamp", "UserId", "_id" };
        private readonly DataSourceInfo _dataSourceInfo;

        //PARAM
        private string _param;

        public TemporalDataPersistenceTime(string connectionString, string dbName, DataSourceInfo dataSourceInfo, string userId, string param)
        {
            _connectionString = connectionString;
            _dbName = dbName;
            _dataSourceProviderString = dataSourceInfo.DataSourcePath;
            _userId = userId;
            _dataSourceInfo = dataSourceInfo;
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(string recordKey)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);

            var collection = db.GetCollection<BsonDocument>(_dataSourceProviderString.ToValidDbString() + "_History");
            var users = db.GetCollection<BsonDocument>("Users");
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
            return list;

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(DateTime timeStamp)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);

            var collection = db.GetCollection<BsonDocument>(_dataSourceProviderString.ToValidDbString() + "_History");
            var users = db.GetCollection<BsonDocument>("Users");
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
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var collection = db.GetCollection<BsonDocument>(tagInfo.DataSourceProviderString.ToValidDbString() + "_History");
            var cursorData = collection.FindAllAs<BsonDocument>();
            string[] exceptfields = { "TimeStamp", "UserId", "_id" };
            var list = new List<Dictionary<string, object>>();

            foreach (var cursor in cursorData)
            {
                cursor["Data"].AsBsonArray.Where(w => w.ToString() != "{ }").Join(tagInfo.Revisions, j1 => j1["_id"].ToString(), j2 => j2, (j1, j2) =>
                {
                    var dict = new Dictionary<string, object> {{"RecordKey", cursor["RecordKey"].ToString()}};
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
        //    var client = new MongoClient(_connectionString);
        //    var mongoServer = client.GetServer();
        //    var db = mongoServer.GetDatabase(_dbName);
        //    var collection = db.GetCollection<BsonDocument>(recordChangedParam.ProviderString.ToValidDbString() + "_History");
        //    if (!string.IsNullOrEmpty(recordChangedParam.OriginalRecordKey) &&
        //        (recordChangedParam.OriginalRecordKey != recordChangedParam.RecordKey))
        //    {
        //        var query = Query.EQ("RecordKey", recordChangedParam.OriginalRecordKey);
        //        var update = Update.Set("RecordKey", recordChangedParam.RecordKey);
        //        collection.Update(query, update, UpdateFlags.Multi);
        //    }
        //   var cursor = collection.FindOne(Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey), Query.LT("Current", _buffer)));
        //    if (cursor == null)
        //    {
        //        var bsDoc = new BsonDocument
        //        {
        //            {"RecordKey", recordChangedParam.RecordKey},
        //            {"Current", 0},
        //           // {"Total", _buffer}
        //        };
        //        var bsItem = new BsonDocument();
        //        AddStructureFields(ref bsItem, recordChangedParam.UserToken);
        //        bsItem.AddRange(recordChangedParam.RecordValues);
        //        var array = new BsonArray();
        //        var bsonStructure = new BsonDocument {{"TimeStamp", null}, {"UserId", null}};
        //        array.Add(bsItem);
        //        for (int i = 1; i < _buffer; i++)
        //            array.Add(bsonStructure);
        //        bsDoc.Add("Data", array);
        //        collection.Insert(bsDoc);
        //    }
        //    else
        //    {
        //        switch (recordChangedParam.ChangedAction)
        //        {
        //            case RecordChangedAction.AddedOrUpdated:
        //                {
        //                    var bsDoc = new BsonDocument();
        //                    AddStructureFields(ref bsDoc, recordChangedParam.UserToken);
        //                    bsDoc.AddRange(recordChangedParam.RecordValues);
        //                    var query = Query.EQ("_id", cursor["_id"]);
        //                    var num = cursor["Current"].AsInt32 + 1;
        //                    var update = Update.Set(string.Format("Data.{0}", num), bsDoc).Set("Current", num);
        //                    var element = cursor["Data"].AsBsonArray.FirstOrDefault(w => w["TimeStamp"].ToNullableUniversalTime() == null);
        //                    if (element == null)
        //                    {
        //                        collection.Update(query, update);
        //                        break;
        //                    }
        //                    var index = cursor["Data"].AsBsonArray.IndexOf(element);
        //                    element["TimeStamp"] = DateTime.Now;
        //                    collection.Update(query, Update.Set(string.Format("Data.{0}", index), element));
        //                    collection.Update(query, update);
        //                    break;
        //                }
        //            case RecordChangedAction.Removed:
        //                {
        //                    var query = Query.And(Query.EQ("_id", cursor["_id"]), Query.EQ("RecordKey", recordChangedParam.RecordKey));
        //                    collection.Update(query, Update.Set("Data.TimeStamp", DateTime.Now),UpdateFlags.Multi);
        //                }
        //                break;
        //        }
        //    }
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var recordsHistory = db.GetCollection<BsonDocument>(tagInfo.DataSourceProviderString.ToValidDbString() + "_History");
            var query = Query.EQ("Data.$.TimeStamp", BsonNull.Value);
            tagInfo.Revisions =
                recordsHistory.Find(query)
                    .Select(s =>s["Data"].AsBsonArray.First(w => w.ToString() != "{ }" && w["TimeStamp"] == BsonNull.Value))
                    .Select(s => s.ToBsonDocument()["_id"].ToString()).ToList();        
            var collection = db.GetCollection<TagInfo>("TagInfo");
            collection.Insert(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var collection = db.GetCollection<TagInfo>("TagInfo");
            var query = Query<TagInfo>.EQ(t => t.TagName, tagInfo.TagName);
            collection.Remove(query);
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var collection = db.GetCollection<TagInfo>("TagInfo");
            return collection.FindAll().SetFields(Fields.Exclude("_id")).ToList();
        }

        void AddStructureFields(ref BsonDocument bsonDocument, string userToken)
        {
            bsonDocument.Add("_id", ObjectId.GenerateNewId());
            bsonDocument.Add("TimeStamp", BsonNull.Value);
            bsonDocument.Add("UserId", string.IsNullOrEmpty(userToken) ? string.Empty : userToken);
        }



        private object ToStrongTypedObject(BsonValue bsonValue, string fieldName)
        {
            if (fieldName == "RecordKey") return bsonValue.ToString();
            var dataType = _dataSourceInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
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
                    return bsonValue.ToLocalTime();
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }
    }
}
