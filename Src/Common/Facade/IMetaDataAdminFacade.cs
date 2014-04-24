using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface IMetaDataAdminFacade  : IMetaDataFacade
    {
        /// <summary>
        ///     Update data source 
        /// </summary>
        /// <param name="dataSource">Data source object with data to update</param>
        /// <param name="oldDataSourceUrn">Providers string that indicates data source that must be updated </param>
        /// <param name="userId">Id that indicates user who make update</param>
        void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userId);

        /// <summary>
        ///     Create data source
        /// </summary>
        /// <param name="dataSource">New datasource object to insert in database</param>
        /// <param name="userId">Id that indicates user who create data source</param>
        /// <returns>Return id that has indicates just created datasource</returns>
        void CreateDataSourceInfo(DataSourceInfo dataSource, string userId);

        /// <summary>
        ///     Delete data source
        /// </summary>
        /// <param name="dataSourceUrn">Provider string that indicates data source</param>
        /// <param name="userId">>Id that indicates user who delete data source</param>
        void DeleteDataSourceInfo(string dataSourceUrn, string userId);

        /// <summary>
        /// Get Worksheet Info by Id
        /// </summary>
        /// <param name="worksheetUrn">Id that indicates worksheet info</param>
        /// <returns>Worksheet Info class</returns>
        WorksheetInfo GetWorksheetInfo(string worksheetUrn);

        
        /// <summary>
        ///     Get all available worksheet infos that user indicated by id has the permission
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <param name="minAccessLevel">Min permission assigned to user id</param>
        /// <returns>Array of worksheet info</returns>
        WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read);

        /// <summary>
        ///     Updates worksheet info 
        /// </summary>
        /// <param name="wsInfo">Worksheet info with data to update</param>
        /// <param name="oldWorksheetUrn">In that indicatew WorksheetInfo to be updated</param>
        /// <param name="userId">Id that indicates user who make update</param>
        void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userId);

        /// <summary>
        ///     Create Worksheet info
        /// </summary>
        /// <param name="wsInfo">New instance of worksheet info</param>
        /// <param name="userId">Id that indicates user who create worksheet</param>
        /// <returns>Object that indicates just created worksheet info</returns>
        void CreateWorksheetInfo(WorksheetInfo wsInfo, string userId);

        /// <summary>
        ///     Delete worksheet info
        /// </summary>
        /// <param name="worksheetUrn">Object that indicates worksheet info to delete</param>
        /// <param name="userId">Id that indicates user who delete worksheet</param>
        void DeleteWorksheetInfo(string worksheetUrn, string userId);

        /// <summary>
        ///     Get all available aggregated worksheet infos that user indicated by id has the permission
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <param name="minAccessLevel">Min permission assigned to user id</param>
        /// <returns>Array of aggregated worksheet info</returns>
        AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read);

        /// <summary>
        ///     Updates worksheet info 
        /// </summary>
        /// <param name="wsInfo">Worksheet info with data to update</param>
        /// <param name="oldWorksheetUrn">In that indicatew WorksheetInfo to be updated</param>
        /// <param name="userId">Id that indicates user who make update</param>
        void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userId);

        /// <summary>
        ///     Create Aggregated Worksheet info
        /// </summary>
        /// <param name="wsInfo">New instance of aggregated worksheet info</param>
        /// <param name="userId">Id that indicates user who create aggregated worksheet</param>
        /// <returns>Object that indicates just created worksheet info</returns>
        void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId);

        /// <summary>
        ///     Delete aggregated worksheet info
        /// </summary>
        /// <param name="worksheetUrn">Object that indicates aggregated worksheet info to delete</param>
        /// <param name="userId">Id that indicates user who delete worksheet</param>
        void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId);

        /// <summary>
        /// Get Aggregated Worksheet Info by Id
        /// </summary>
        /// <param name="worksheetUrn">Id that indicates aggregated worksheet info</param>
        /// <returns>Worksheet Info class</returns>
        AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn);

    }
}
