using System;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface IMetaDataProvider
    {
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


        Action<DataSourceInfo> OnDataSourceInfoChanged { get; set; }
    }
}
