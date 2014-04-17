namespace ReactiveWorksheets.Client.SignalR
{
    public static class FacadeFactory
    {
        private static CommandFacade _commandFacade;
        public static CommandFacade CreateReactiveDataCommandFacade(string connectionString)
        {
            return _commandFacade ?? (_commandFacade = new CommandFacade(connectionString));
        }

        private static ReactiveDataQueryFacade _reactiveDataQueryFacade;
        public static ReactiveDataQueryFacade CreateReactiveDataQueryFacade(string connectionString)
        {
            return _reactiveDataQueryFacade ?? (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(connectionString));
        }

        private static TemporalDataQueryFacade _temporalDataQueryFacade;
        public static TemporalDataQueryFacade CreateTemporalDataQueryFacade(string connectionString)
        {
            return _temporalDataQueryFacade ?? (_temporalDataQueryFacade = new TemporalDataQueryFacade(connectionString));
        }

        private static MetaDataFacade _metaDataFacade;
        public static MetaDataFacade CreateMetaDataFacade(string connectionSring)
        {
            return _metaDataFacade ?? (_metaDataFacade = new MetaDataFacade(connectionSring));
        }

        private static SearchFacade _searchFacade;
        public static SearchFacade CreateSearchFacade(string connectionString)
        {
            return _searchFacade ?? (_searchFacade = new SearchFacade(connectionString));
        }

        private static SecurityFacade _securityFacade;
        public static SecurityFacade CreateSecurityFacade(string connectionString)
        {
            return _securityFacade ?? (_securityFacade = new SecurityFacade(connectionString));
        }
    }
}
