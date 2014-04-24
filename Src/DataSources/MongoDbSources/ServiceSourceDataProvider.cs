using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Server.Core.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources
{
    public class ServiceSourceDataProvider : IServiceDataProvider
    {
        private readonly string _connectionString;

        public ServiceSourceDataProvider(string dataConnectionString)
        {
            _connectionString = dataConnectionString;
        }

        public ServiceSourceInfo ServiceSourceInfo;

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        public IEnumerable<Dictionary<string, object>> GetData(IEnumerable<Dictionary<string, object>> inParams, Action<string, string> onError = null)
        {
            //var db = MongoDatabase.Create(_connectionString);
            //var collection = db.GetCollection(ServiceSourceInfo.DataSourcePath.ToValidDbString() + "_SSData");
            ////MongoCursor<BsonDocument> cursor;
            ////var query = CreateFilterRuleQuery(filterRules);
            ////if (string.IsNullOrEmpty(query))
            ////{
            ////    cursor = collection.FindAll();
            ////}
            ////else
            ////{
            ////    var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
            ////    cursor = collection.FindAs<BsonDocument>(qwraper);
            ////}
            ////var data = cursor.SetFields(Fields.Exclude("_id"));
            ////var listOfRecords = data.Select(bsdocumnet =>
            ////        bsdocumnet.ToDictionary(doc => doc.Name, doc => ToStrongTypedObject(doc.Value, doc.Name)))
            ////        .ToList();
            //return listOfRecords;
            throw new Exception();
        }

        public void RequestCalculation(DataSourceInfo dataSourceInfo, RecordChangedParam recordChangedParam, Action<string, RecordChangedParam> onSuccess,
            Action<string, Exception> onFail)
        {
            foreach (var serviceSourceRelationship in dataSourceInfo.ServiceRelations)
            {
                var relationUrn = serviceSourceRelationship.Key;
                var inputParameters =
                     serviceSourceRelationship.Value.Relations.ToDictionary(
                         serviceRelationElement => serviceRelationElement.ServiceParamName,
                         serviceRelationElement => recordChangedParam.RecordValues[serviceRelationElement.Field.Name]);
                var results = new PythonEngine().GetFormulaResult(ServiceSourceInfo.Script, inputParameters,
                    ServiceSourceInfo.OutParams.ToDictionary(y => y.Name, y => y.Value), (s, e) => { });
                if (results != null)
                {
                    var recordValues = recordChangedParam.RecordValues;
                    foreach (var result in results)
                    {
                        recordValues[
                            dataSourceInfo.Fields.First(
                                f => f.Value.RelationUrn == relationUrn && f.Value.RelatedFieldName == result.Key)
                                .Key] =
                            result.Value;
                    }
                    recordChangedParam.ChangedPropertyNames =
                        dataSourceInfo.Fields.Where(w => w.Value.RelationUrn == relationUrn)
                            .Select(s => s.Key)
                            .ToArray();
                    onSuccess("Success", recordChangedParam);
                }

                else
                {
                    onFail("error", new Exception());
                }
            }
        }

        private MongoCollection<BsonDocument> GetCollection(string name)
        {
            var db = MongoDatabase.Create(_connectionString);
            if (!db.CollectionExists(name))
                throw new InvalidDataException("No collection with such name exists!!!");
            var collection = db.GetCollection(name);
            return collection;
        }
    }
}
