using System;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;

namespace ReactiveWorksheets.Client.SignalR
{
    public static class FacadesFactory
    {
        private static string _serverUrl;

        public static void SetServerUrl(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public static ICommandFacade CreateCommandFacade()
        {
            if(string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new CommandFacade(_serverUrl);
        }

        private static object _createReactiveDataQueryFacadeLock = new object();
        public static IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");
            lock (_createReactiveDataQueryFacadeLock)
            {
                var obj = Task.Run(() => new ReactiveDataQueryFacade(_serverUrl)).Result;
                //return new ReactiveDataQueryFacade(_serverUrl);
                return obj;
            }
        }

        public static ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new TemporalDataQueryFacade(_serverUrl);
        }

        public static IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl);
        }

        public static IMetaDataFacade CreateMetaDataFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new MetaDataFacade(_serverUrl);
        }

        public static ISearchFacade CreateSearchFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SearchFacade(_serverUrl);
        }

        public static ISecurityFacade CreateSecurityFacade()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new ApplicationException("Server Url is not initialized in bootstrapper");

            return new SecurityFacade(_serverUrl);
        }
    }
}
