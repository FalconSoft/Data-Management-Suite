using System;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface IMetaDataFacade : IDisposable
    {
        /// <summary>
        ///     Get all available for user designated by id with such permission datasources 
        /// </summary>
        /// <param name="userId">User id </param>
        /// <param name="minAccessLevel">Min permission assigned to user id</param>
        /// <returns>Array of data sources on which the user has specified a minimum permission </returns>
        DataSourceInfo[] GetAvailableDataSources(string userId, AccessLevel minAccessLevel = AccessLevel.Read);

        /// <summary>
        ///     Get datasource with defined provider string
        /// </summary>
        /// <param name="dataSourceUrn">Datasource's provider string</param>
        /// <returns>Data source with given provider string</returns>
        DataSourceInfo GetDataSourceInfo(string dataSourceUrn);
        
        /// <summary>
        /// Get all Dependent DataSources that lays "below" requested DataSource
        /// </summary>
        /// <param name="dataSourceUrn">Requested DataSource`s provider string</param>
        /// <returns></returns>
        DataSourceInfo[] GetDependentDataSources(string dataSourceUrn);
        
        /// <summary>
        /// Notification that ObjectInfo changed
        /// </summary>
        event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;
    }

    public class SourceObjectChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Changed Object
        /// </summary>
        public object SourceObjectInfo { get; set; }

        /// <summary>
        /// Providers string that indicates object source that must be updated
        /// </summary>
        public string OldObjectUrn { get; set; }

        /// <summary>
        /// Type of Changed Object
        /// </summary>
        public ChangedObjectType ChangedObjectType { get; set; }

        /// <summary>
        /// Specified type of raized action
        /// </summary>
        public ChangedActionType ChangedActionType { get; set; }
    }

    public enum ChangedActionType
    {
        Create, Update, Delete
    }

    public enum ChangedObjectType
    {
        DataSourceInfo, WorksheetInfo, AggregatedWorksheetInfo
    }
}
