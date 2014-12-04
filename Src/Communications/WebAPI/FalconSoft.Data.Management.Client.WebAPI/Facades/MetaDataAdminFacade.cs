using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class MetaDataAdminFacade : WebApiClientBase, IMetaDataAdminFacade
    {

        private HubConnection _connection = null;

        public MetaDataAdminFacade(string url, ILogger log)
            : base(url, "MetaDataApi", log)
        {
            ConnectAsync();
            //_rabbitMQClient = rabbitMQClient;

            //_rabbitMQClient.SubscribeOnExchange<SourceObjectChangedEventArgs>(MetadataExchangeName, "fanout", "", oic => ObjectInfoChanged(this, oic));

            //_rabbitMQClient.SubscribeOnExchange(ExceptionsExchangeName, "fanout", "", ErrorMessageHandledAction);
        }

        private async void ConnectAsync()
        {
            _connection = new HubConnection("http://localhost:8082");
            //Connection.Closed += Connection_Closed;
            var hubProxy = _connection.CreateHubProxy("MetaDataHub");
            //Handle incoming event from server: use Invoke to write to console from SignalR's thread
            hubProxy.On<string>("ObjectInfoChanged", OnObjectInfoChanged);

            try
            {
                await _connection.Start();
            }
            catch (HttpRequestException)
            {
                Trace.WriteLine("Unable to connect to server: Start server before connecting clients.");
                //No connection: Don't enable Send button or show chat UI
                return;
            }

            Trace.WriteLine("Connected to MetaDataHub server ");
        }

        private void OnObjectInfoChanged(string msg)
        {
            var ev = ObjectInfoChanged;
            if (ev != null)
            {
                var eventArg = JsonConvert.DeserializeObject<SourceObjectChangedEventArgs>(msg);
                ev(this, eventArg);
            }
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<DataSourceInfo[]>("GetAvailableDataSources",
                new Dictionary<string, object>
                {
                    { "userToken", userToken },
                    { "minAccessLevel", minAccessLevel }
                });
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return GetWebApiCall<DataSourceInfo>("GetDataSourceInfo",
                new Dictionary<string, object>
                {
                    {"dataSourceUrn", dataSourceUrn},
                    {"userToken", userToken}
                });
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            PostWebApiCall("UpdateDataSourceInfo", dataSource, new Dictionary<string, object>
            {
                {"oldDataSourceUrn", oldDataSourceUrn},
                {"userToken", userToken}
            });
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            PostWebApiCall("CreateDataSourceInfo", dataSource, new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            PostWebApiCall("DeleteDataSourceInfo",  new Dictionary<string, object>
            {
                {"dataSourceUrn", dataSourceUrn},
                {"userToken", userToken}
            });
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            return GetWebApiCall<WorksheetInfo>("GetWorksheetInfo",
                new Dictionary<string, object>
                {
                    { "worksheetUrn", worksheetUrn }, 
                    { "userToken", userToken }
                });
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<WorksheetInfo[]>("GetAvailableWorksheets",
                new Dictionary<string, object>
                {
                    { "userToken", userToken }, 
                    { "minAccessLevel", minAccessLevel }
                });
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            PostWebApiCall("UpdateWorksheetInfo", wsInfo, new Dictionary<string, object>
            {
                {"oldWorksheetUrn", oldWorksheetUrn},
                {"userToken", userToken}
            });
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            PostWebApiCall("CreateWorksheetInfo", wsInfo, new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            PostWebApiCall("DeleteWorksheetInfo",  new Dictionary<string, object>
            {
                {"worksheetUrn", worksheetUrn},
                {"userToken", userToken}
            });
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<AggregatedWorksheetInfo[]>("GetAvailableAggregatedWorksheets",
                new Dictionary<string, object>
                {
                    {"userToken", userToken}, 
                    {"minAccessLevel", minAccessLevel}
                });
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            PostWebApiCall("UpdateAggregatedWorksheetInfo", wsInfo, new Dictionary<string, object>
            {
                {"oldWorksheetUrn", oldWorksheetUrn},
                {"userToken", userToken}
            });
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            PostWebApiCall("CreateAggregatedWorksheetInfo", wsInfo, new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            PostWebApiCall("DeleteAggregatedWorksheetInfo",  new Dictionary<string, object>
            {
                {"worksheetUrn", worksheetUrn},
                {"userToken", userToken}
            });
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            return GetWebApiCall<AggregatedWorksheetInfo>("GetAggregatedWorksheetInfo",
              new Dictionary<string, object>
              {
                  { "worksheetUrn", worksheetUrn }, 
                  { "userToken", userToken }
              });
        }

        public ServerInfo GetServerInfo()
        {
            return GetWebApiCall<ServerInfo>("GetServerInfo");
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Stop();
                _connection.Dispose();
            }
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}