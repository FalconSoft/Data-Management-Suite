using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.TemporalData
{
    public class TemporalDataPersistence : ITemporalDataPersistense
    {
        private readonly string _dataSourceProviderString;
        private readonly string _userId;
        private readonly string[] _dbfields = { "RecordKey", "ValidFrom", "ValidTo", "UserId", "_id" };
        private readonly DataSourceInfo _dataSourceInfo;
        private readonly LiveDataMongoCollections _mongoCollections;
        private readonly MetaDataMongoCollections _metaMongoCollections;
        private readonly MongoCollection<BsonDocument> _historyCollection; 

        public TemporalDataPersistence(LiveDataMongoCollections mongoCollections, MetaDataMongoCollections metaMongoCollections, DataSourceInfo dataSourceInfo, string userId)
        {
            _mongoCollections = mongoCollections;
            _metaMongoCollections = metaMongoCollections;
            _dataSourceProviderString = dataSourceInfo.DataSourcePath;
            _userId = userId;
            _dataSourceInfo = dataSourceInfo;

            _historyCollection = _mongoCollections.GetHistoryDataCollection(_dataSourceProviderString);            
        }


        public IEnumerable<Dictionary<string, object>> GetTemporalData(string recordKey)
        {
            var users = _metaMongoCollections.Users;
            var cursorData = _historyCollection.FindAllAs<BsonDocument>();
            var cursorUser = users.FindAllAs<BsonDocument>();

            var list = new List<Dictionary<string, object>>();
            foreach (var cdata in cursorData)
            {
                if (cdata["RecordKey"].AsString != recordKey) continue;
                var user = cursorUser.FirstOrDefault(f => f["_id"].ToString() == cdata["UserId"].ToString());
                var loginname = user == null ? _dataSourceProviderString : user["LoginName"].ToString();
                var dict = new Dictionary<string, object>();
                dict.Add("LoginName", loginname);
                dict.Add("TimeStamp", cdata["ValidFrom"].ToLocalTime());
                foreach (var data in cdata)
                {
                    if (_dbfields.All(a => a != data.Name))
                        dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                }
                list.Add(dict);
            }
            return list;

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalData(DateTime timeStamp, string urn)
        {
            var users = _metaMongoCollections.Users;
            var cursorData = _historyCollection.FindAllAs<BsonDocument>();
            var cursorUser = users.FindAllAs<BsonDocument>();
            var list = new List<Dictionary<string, object>>();

            foreach (var cdata in cursorData)
            {
                if (cdata["ValidTo"].ToUniversalTime() > timeStamp) continue;
                var user = cursorUser.FirstOrDefault(f => f["_id"].ToString() == cdata["UserId"].ToString());
                var loginname = user == null ? _dataSourceProviderString : user["LoginName"].ToString();
                var dict = new Dictionary<string, object>();
                dict.Add("LoginName", loginname);
                dict.Add("TimeStamp", cdata["ValidFrom"].ToLocalTime());
                foreach (var data in cdata)
                {
                    if (_dbfields.All(a => a != data.Name))
                        dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
                }
                list.Add(dict);
            }
            return list;

        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByTag(TagInfo tagInfo)
        {
            //ConnectToDb();
            //var collection = _mongoDatabase.GetCollection<BsonDocument>(tagInfo.DataSourceProviderString.ToValidDbString() + "_History");
            //var cursorData = collection.FindAllAs<BsonDocument>();
            //string[] exceptfields = { "ValidFrom", "ValidTo", "UserId", "_id" };
            //var list = new List<Dictionary<string, object>>();
            //cursorData.Join(tagInfo.Revisions, j1 => j1["_id"].ToString(), j2 => j2, (j1, j2) =>
            //{
            //    var dict = new Dictionary<string, object>();
            //    foreach (var data in j1)
            //    {
            //        if (exceptfields.All(a => a != data.Name))
            //            dict.Add(data.Name, ToStrongTypedObject(data.Value, data.Name));
            //    }
            //    list.Add(dict);
            //    return j2;
            //}).Count();
            //return list;
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(object revisionId = null)
        {
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions()
        {
            return null;
        }

        public object AddRevision(string urn, string userId)
        {
            return new Guid();
        }

        public void SaveTempotalData(RecordChangedParam recordChangedParam, object revisionId)
        {
            switch (recordChangedParam.ChangedAction)
            {
                case RecordChangedAction.AddedOrUpdated:
                    {
                        var bsDoc = new BsonDocument();
                        AddSystemFields(ref bsDoc, recordChangedParam.RecordKey, recordChangedParam.UserToken);
                        bsDoc.AddRange(recordChangedParam.RecordValues);
                        if (string.IsNullOrEmpty(recordChangedParam.OriginalRecordKey))
                        {
                            var query = Query.And(Query.EQ("RecordKey", recordChangedParam.RecordKey),
                                Query.EQ("ValidTo", BsonNull.Value));
                            var oldrecord = _historyCollection.FindOne(query);
                            if (oldrecord == null)
                            {
                                _historyCollection.Insert(bsDoc);
                                break;
                            }
                            oldrecord["ValidTo"] = bsDoc["ValidFrom"];
                            _historyCollection.Save(oldrecord);
                        }
                        _historyCollection.Insert(bsDoc);
                        break;
                    }

                case RecordChangedAction.Removed:
                    {
                        var query = Query.And(Query.EQ("RecordKey", recordChangedParam.OriginalRecordKey), Query.EQ("ValidTo", BsonNull.Value));
                        _historyCollection.Update(query, Update.Set("ValidTo", DateTime.Now));
                    }
                    break;
            }
        }

        public void UpdateTemporalData(RecordChangedParam recordChangedParam)
        {
            throw new NotImplementedException();
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            _mongoCollections.TagInfos.Insert(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var query = Query<TagInfo>.EQ(t => t.TagName, tagInfo.TagName);
            _mongoCollections.TagInfos.Remove(query);
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            return _mongoCollections.TagInfos.FindAll().SetFields(Fields.Exclude("_id")).ToList();
        }

        void AddSystemFields(ref BsonDocument bsonDocument, string recordKey, string userToken)
        {
            bsonDocument.Add("RecordKey", new BsonString(recordKey));
            bsonDocument.Add("ValidFrom", DateTime.Now);
            bsonDocument.Add("ValidTo", BsonNull.Value);
            bsonDocument.Add("UserId", string.IsNullOrEmpty(userToken) ? BsonNull.Value.ToString() : userToken);
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
