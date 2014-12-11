using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class DataProvidersCatalog : IDataProvidersCatalog
    {
        private readonly MongoDbCollections _mongoCollections;

        public DataProvidersCatalog(string connectionString)
        {
            _mongoCollections = new MongoDbCollections(connectionString);
        }

        public IEnumerable<DataProvidersContext> GetProviders()
        {
            var collectionDs = _mongoCollections.TableInfos.FindAll().ToArray();
            var listDataProviders = new List<DataProvidersContext>();

            foreach (var dataSource in collectionDs)
            {
                IDataProvider dataProvider;
                if (string.IsNullOrEmpty(dataSource.Description))
                    dataProvider = new DataProvider(_mongoCollections, dataSource);
                else
                {
                    dataProvider = new PythonDataProvider(dataSource);
                }

                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = dataSource.DataSourcePath,
                        DataProvider = dataProvider,
                        ProviderInfo = dataSource,
                        MetaDataProvider = new MetaDataProvider(_mongoCollections)
                    };

                var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
                if (metaDataProvider != null & (dataProvider is DataProvider)) metaDataProvider.OnDataSourceInfoChanged = (dataProvider as DataProvider).UpdateSourceInfo;
                listDataProviders.Add(dataProviderContext);
            }
            return listDataProviders;
        }

        public event EventHandler<DataProvidersContext> DataProviderAdded;

        public event EventHandler<StringEventArg> DataProviderRemoved;

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            var collection = _mongoCollections.TableInfos;
            dataSource.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(dataSource);

            IDataProvider dataProvider;
            if (string.IsNullOrEmpty(dataSource.Description))
                dataProvider = new DataProvider(_mongoCollections, dataSource);
            else
            {
                dataProvider = new PythonDataProvider(dataSource);
            }

            //var dataProvider = new DataProvider(_connectionString, dataSource);

            var dataProviderContext = new DataProvidersContext
                {
                    Urn = dataSource.DataSourcePath,
                    DataProvider = dataProvider,
                    ProviderInfo = dataSource,
                    MetaDataProvider = new MetaDataProvider(_mongoCollections)
                };

            var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
            if (metaDataProvider != null & (dataProvider is DataProvider)) metaDataProvider.OnDataSourceInfoChanged = (dataProvider as DataProvider).UpdateSourceInfo;

            DataProviderAdded(this, dataProviderContext);
            return collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", Utils.GetNamePart(dataSource.DataSourcePath)),
                                                                 Query.EQ("Category", Utils.GetCategoryPart(dataSource.DataSourcePath))));
        }

        public void RemoveDataSource(string dataSourceProviderString)
        {
            _mongoCollections.GetDataCollection(dataSourceProviderString)
                           .Drop();
            _mongoCollections.GetHistoryDataCollection(dataSourceProviderString)
                          .Drop();
            _mongoCollections.TableInfos
                          .Remove(Query.And(Query.EQ("Name", Utils.GetNamePart(dataSourceProviderString)),
                                            Query.EQ("Category", Utils.GetCategoryPart(dataSourceProviderString))));
            DataProviderRemoved(this, new StringEventArg(dataSourceProviderString));
        }

    }
}