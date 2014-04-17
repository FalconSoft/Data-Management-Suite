using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface IMetaDataProvider
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
        /// <param name="dataSourceProviderString">Datasource's provider string</param>
        /// <returns>Data source with given provider string</returns>
        DataSourceInfo GetDataSourceInfo(string dataSourceProviderString);

        /// <summary>
        ///     Update data source 
        /// </summary>
        /// <param name="dataSource">Data source object with data to update</param>
        /// <param name="oldDataSourceProviderString">Providers string that indicates data source that must be updated </param>
        /// <param name="userId">Id that indicates user who make update</param>
        void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceProviderString, string userId);

        /// <summary>
        ///     Create data source
        /// </summary>
        /// <param name="dataSource">New datasource object to insert in database</param>
        /// <param name="userId">Id that indicates user who create data source</param>
        /// <returns>Return id that has indicates just created datasource</returns>
        DataSourceInfo CreateDataSourceInfo(DataSourceInfo dataSource, string userId);

        /// <summary>
        ///     Delete data source
        /// </summary>
        /// <param name="dataSourceProviderString">Provider string that indicates data source</param>
        /// <param name="userId">>Id that indicates user who delete data source</param>
        void DeleteDataSourceInfo(string dataSourceProviderString, string userId);

        /// <summary>
        ///     Get all available Service source infos indicated bu user id
        /// </summary>
        /// <param name="userId">Id that indicates user</param>
        /// <returns>Array of service source infos</returns>
        ServiceSourceInfo[] GetAllServiceSourceInfos(string userId);

        /// <summary>
        ///     Get Service source info by service source name
        /// </summary>
        /// <param name="name">String that indicates service source info</param>
        /// <returns>Instance of service source info that is indicated by name</returns>
        ServiceSourceInfo GetServiceSourceInfo(string name);

        /// <summary>
        ///     Create service source
        /// </summary>
        /// <param name="serviceSourceInfo">Instance of new service source</param>
        /// <param name="userId">Id that indicates user who create Service source</param>
        /// <returns>Object that indicates just created service source</returns>
        ServiceSourceInfo CreateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId);

        /// <summary>
        ///     Update service source
        /// </summary>
        /// <param name="serviceSourceInfo">Service source info with data to update</param>
        /// <param name="userId">Id that indicates user who make update</param>
        void UpdateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId);

        /// <summary>
        ///     Delete Service source
        /// </summary>
        /// <param name="serviceSourceName">String that indicates service source by name</param>
        /// <param name="userId">Id that indicates user who delete service source</param>
        void DeleteServiceSourceInfo(string serviceSourceName, string userId);

    }
}
