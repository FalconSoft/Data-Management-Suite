﻿using System;
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
        private const string HubName = "MetaDataHub";
        private readonly SignalRHub _signalRHub;


        public MetaDataAdminFacade(string url, string pushUrl, ILogger log)
            : base(url, "MetaDataApi", log)
        {
            _signalRHub = new SignalRHub(pushUrl, HubName, log, hubProxy => hubProxy.On<string, string, string, string>("ObjectInfoChanged", OnObjectInfoChanged));
        }

        private void OnObjectInfoChanged(string pushKey, string oldUri, string changedActionType, string changedObjectType)
        {
            var ev = ObjectInfoChanged;
            if (ev != null)
            {
                var msg = GetWebApiCall<string>("GetPushedMetaDataObject", new Dictionary<string, object>
                {
                    { "userToken", "" },
                    { "pushKey", pushKey }
                });

                // this whole workaround types is due to bad design!
                // SourceObjectInfo should not have three objects in one
                var eventArg = new SourceObjectChangedEventArgs
                {
                    OldObjectUrn = oldUri,
                    ChangedActionType = (ChangedActionType)Enum.Parse(typeof(ChangedActionType), changedActionType),
                    ChangedObjectType = (ChangedObjectType)Enum.Parse(typeof(ChangedObjectType), changedObjectType)
                };

                if (eventArg.ChangedObjectType == ChangedObjectType.DataSourceInfo)
                {
                    eventArg.SourceObjectInfo = JsonConvert.DeserializeObject<DataSourceInfo>(msg);
                }
                else if (eventArg.ChangedObjectType == ChangedObjectType.WorksheetInfo)
                {
                    eventArg.SourceObjectInfo = JsonConvert.DeserializeObject<WorksheetInfo>(msg);
                }
                else if (eventArg.ChangedObjectType == ChangedObjectType.AggregatedWorksheetInfo)
                {
                    eventArg.SourceObjectInfo = JsonConvert.DeserializeObject<AggregatedWorksheetInfo>(msg);
                }

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

        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}