using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;

namespace ReactiveWorksheets.Facade.Tests
{
    internal class SimpleDataSourceTest : IDisposable
    {
        private DataSourceInfo _dataSourceInfo;
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ICommandFacade _commandFacade;
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        private readonly string _userToken;

        public SimpleDataSourceTest(DataSourceInfo dataSourceInfo, IFacadesFactory facadesFactory,User user)
        {
            _dataSourceInfo = dataSourceInfo;
            _metaDataAdminFacade = facadesFactory.CreateMetaDataAdminFacade();
            _commandFacade = facadesFactory.CreateCommandFacade();
            _reactiveDataQueryFacade = facadesFactory.CreateReactiveDataQueryFacade();
            _temporalDataQueryFacade = facadesFactory.CreateTemporalDataQueryFacade();
            _userToken = user.Id;
            _metaDataAdminFacade.CreateDataSourceInfo(_dataSourceInfo, user.Id);
        }

        public WorksheetInfo CreateWorksheetInfo(string name, string category, List<ColumnInfo> columns, User user)
        {
            var worksheet = new WorksheetInfo
            {
                Category = category,
                Name = name,
                Columns = columns
            };
            _metaDataAdminFacade.CreateWorksheetInfo(worksheet,user.Id);

            return _metaDataAdminFacade.GetWorksheetInfo(worksheet.DataSourcePath, user.Id);
        }

        public void SubmitData(string comment, IEnumerable<Dictionary<string, object>> changedData = null, IEnumerable<string> deleted = null)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _commandFacade.SubmitChanges(_dataSourceInfo.DataSourcePath, comment, changedData, deleted, r =>
            {
                Console.WriteLine("Data submited successfull");
                tcs.SetResult(r);
            }, ex =>
            {
                Console.WriteLine("Data submit failed");
                tcs.SetException(ex);
            });

            task.Wait();
        }

        public IEnumerable<Dictionary<string, object>> GetData(FilterRule[] filterRules = null)
        {
            return _reactiveDataQueryFacade.GetData(_userToken, _dataSourceInfo.DataSourcePath, filterRules: filterRules);
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(FilterRule[] filterRules = null)
        {
            return _reactiveDataQueryFacade.GetDataChanges(_userToken, _dataSourceInfo.DataSourcePath, fields: filterRules);
        }

        public IEnumerable<Dictionary<string,object>> GetHistory(string recordKey)
        {
            return _temporalDataQueryFacade.GetRecordsHistory(_dataSourceInfo, recordKey);
        }

        public DataSourceInfo UpdateDataSourceInfo(DataSourceInfo dataSourceInfo,User user)
        {
            _metaDataAdminFacade.UpdateDataSourceInfo(dataSourceInfo,_dataSourceInfo.DataSourcePath,user.Id);
            _dataSourceInfo = _metaDataAdminFacade.GetDataSourceInfo(dataSourceInfo.DataSourcePath, user.Id);
            return _dataSourceInfo;
        }

        public void RemoveWorksheet(WorksheetInfo worksheetInfo, User user)
        {
            _metaDataAdminFacade.DeleteWorksheetInfo(worksheetInfo.DataSourcePath,user.Id);
        }

        public void RemoveDatasourceInfo(User user)
        {
            _metaDataAdminFacade.DeleteDataSourceInfo(_dataSourceInfo.DataSourcePath,user.Id);
        }

        public void Dispose()
        {
            _commandFacade.Dispose();
            _reactiveDataQueryFacade.Dispose();
            _metaDataAdminFacade.Dispose();
        }
    }
}
