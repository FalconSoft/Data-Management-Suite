using System;
using System.Configuration;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Core;

using FalconSoft.ReactiveWorksheets.Core.MessageBus;
using FalconSoft.ReactiveWorksheets.Core.ReactiveEngine;
using FalconSoft.ReactiveWorksheets.MongoDbSources;
using FalconSoft.ReactiveWorksheets.Persistence;
using FalconSoft.ReactiveWorksheets.Persistence.LiveData;
using FalconSoft.ReactiveWorksheets.Persistence.MetaData;
using FalconSoft.ReactiveWorksheets.Persistence.SearchIndexes;
using FalconSoft.ReactiveWorksheets.Persistence.Security;
using FalconSoft.ReactiveWorksheets.Persistence.TemporalData;
using FalconSoft.ReactiveWorksheets.Server.Core;
using FalconSoft.ReactiveWorksheets.Server.Core.CommandsAggregator;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using ReactiveWorksheets.ExternalDataSources;

namespace FalconSoft.ReactiveWorksheets.Server.Bootstrapper
{
    public static class ServerApp
    {
        private static ICommandsAggregator _commandAggregator;

        private static IProvidersRegistry _providersRegistry;

        private static IDataProvidersCatalog[] _dataProvidersCatalogs;

        private static IReactiveEngine _reactiveEngine;

        private static IMetaDataAdminFacade _metaDataFacade;

        private static IReactiveDataQueryFacade _dataQueryFacade;

        private static ITemporalDataQueryFacade _temporalQueryFacade;

        private static ICommandFacade _commandFacade;

        private static ISearchFacade _searchFacade;

        private static ISecurityFacade _securityFacade;

        private static Func<string, ILiveDataPersistence> _liveDataPersistenceFactory;

        private static IWorksheetPersistence _worksheetsPersistence;

        private static Func<DataSourceInfo, ITemporalDataPersistense> _temporalDataPersistenseFactory;

        private static ISearchIndexPersistence _searchIndexPersistence;

        private static ISecurityPersistence _securityPersistence;

        private static IMetaDataPersistence _metaDataPersistence;

        private static IMetaDataProvider _metaDataProvider;

        private static IMessageBus _messageBus;

        private static ILogger _logger;

        public static IMessageBus MessageBus
        {
            get
            {
                return _messageBus ?? (_messageBus = new MessageBus());
            }
        }

        public static IMetaDataAdminFacade MetaDataFacade
        {
            get
            {
                return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(ProvidersRegistry, WorksheetsPersistence, MetaDataPersistence));
            }
        }

        public static IReactiveDataQueryFacade ReactiveDataQueryFacade
        {
            get
            {
                return _dataQueryFacade ?? (_dataQueryFacade = new ReactiveDataFacade(MessageBus, LiveDataPersistenceFactory, ReactiveEngine));
            }
        }

        public static ITemporalDataQueryFacade TemporalQueryFacade
        {
            get
            {
                return _temporalQueryFacade ??
                       (_temporalQueryFacade = new TemporalDataQueryFacade(MessageBus, TemporalDataPersistenseFactory,ProvidersRegistry));
            }
        }

        public static ICommandFacade CommandFacade
        {
            get
            {
                return _commandFacade ?? (_commandFacade = new CommandFacade(CommandAggregator));
            }
        }
        
        public static ISearchFacade SearchFacade
        {
            get
            {
                return _searchFacade ?? (_searchFacade = new SearchFacade(MessageBus, SearchIndexPersistence, MetaDataFacade));
            }
        }

        public static ISecurityFacade SecurityFacade
        {
            get
            {
                return _securityFacade ?? (_securityFacade = new SecurityFacade(SecurityPersistence));
            }
        }

        public static IDataProvidersCatalog[] DataProvidersCatalogs
        {
            get
            {
                return _dataProvidersCatalogs ??
                       (_dataProvidersCatalogs = new IDataProvidersCatalog[] { new DataProvidersCatalog(ConfigurationManager.AppSettings["MongoDataConnectionString"]), new ExternalProviderCatalog() });
            }
        }

        public static IReactiveEngine ReactiveEngine
        {
            get
            {
                return _reactiveEngine ?? (_reactiveEngine = new ReactiveEngine(MetaDataFacade, LiveDataPersistenceFactory, new DenormalizedBusPublisher(MessageBus), Logger));
            }
        }

        public static IProvidersRegistry ProvidersRegistry
        {
            get
            {
                return _providersRegistry ?? (_providersRegistry = new ProvidersRegistry {DataProvidersCatalog = DataProvidersCatalogs.First()});
            }  // TODO
        }

        public static ICommandsAggregator CommandAggregator
        {
            get
            {
                return _commandAggregator ??
                       (_commandAggregator = new CommandAggregator(ProvidersRegistry, LiveDataPersistenceFactory,
                                                                   TemporalDataPersistenseFactory,
                                                                   DataProvidersCatalogs, ReactiveEngine, MetaDataPersistence, DefaultMetaDataProvider, Logger));
            }
        }

        public static IWorksheetPersistence WorksheetsPersistence
        {
            get
            {
                return _worksheetsPersistence ??
                       (_worksheetsPersistence = new WorksheetPersistence(ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"]));
            }
        }

        public static Func<string, ILiveDataPersistence> LiveDataPersistenceFactory
        {
            get
            {
                // TODO : this has to move to separate method and user should be able to specify specific connection string for each dataprovider or use default one 
                return _liveDataPersistenceFactory ?? (_liveDataPersistenceFactory = s => new LiveDataPersistence(ConfigurationManager.AppSettings["PersistenceDataConnectionString"], s.GetName()));
            }
        }

        public static Func<DataSourceInfo, ITemporalDataPersistense> TemporalDataPersistenseFactory
        {
            get
            {
                return _temporalDataPersistenseFactory ?? (_temporalDataPersistenseFactory = s =>
                {
                    switch (s.HistoryStorageType)
                    {
                       case HistoryStorageType.Buffer:
                        {
                            return
                                new TemporalDataPersistenceBuffer(ConfigurationManager.AppSettings["PersistenceDataConnectionString"], s, "0", 
                                    string.IsNullOrEmpty(s.HistoryStorageTypeParam) ? 100 : int.Parse(s.HistoryStorageTypeParam));
                        }
                       case HistoryStorageType.Event:
                        {
                            return new TemporalDataPersistence(ConfigurationManager.AppSettings["PersistenceDataConnectionString"], s, "0");
                        }
                       case HistoryStorageType.Time:
                        {
                            //TODO will be soon 
                            return null;
                        }
                    }
                    //DEFAULT RETURN
                    return new TemporalDataPersistenceBuffer(ConfigurationManager.AppSettings["PersistenceDataConnectionString"], s, "0", 100);
                });
                                                                                         
            }
        }

        public static ISearchIndexPersistence SearchIndexPersistence
        {
            get
            {
                return _searchIndexPersistence ??
                       (_searchIndexPersistence = new SearchIndexPersistence(ConfigurationManager.AppSettings["PersistenceDataConnectionString"], ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"]));
            }
        }

        public static ISecurityPersistence SecurityPersistence
        {
            get
            {
                return _securityPersistence ??
                       (_securityPersistence = new SecurityPersistence(ConfigurationManager.AppSettings["PersistenceDataConnectionString"]));
            }
        }

        public static IMetaDataPersistence MetaDataPersistence
        {
            get
            {
                return _metaDataPersistence ??
                       (_metaDataPersistence = new MetaDataPersistence(ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"]));
            }
        }

        public static IMetaDataProvider DefaultMetaDataProvider
        {
            get
            {
                return _metaDataProvider ??
                       (_metaDataProvider = new MetaDataProvider(ConfigurationManager.AppSettings["MongoDataConnectionString"]));
            }
        }

        public static ILogger Logger
        {
            get
            {
                return _logger ?? (_logger = new Logger());
            }
        }
    }
}
