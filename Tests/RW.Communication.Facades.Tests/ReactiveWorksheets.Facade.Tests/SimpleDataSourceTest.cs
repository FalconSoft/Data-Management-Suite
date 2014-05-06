﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace ReactiveWorksheets.Facade.Tests
{
    internal class SimpleDataSourceTest : IDisposable
    {
        private DataSourceInfo _dataSourceInfo;
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ICommandFacade _commandFacade;
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        public SimpleDataSourceTest(DataSourceInfo dataSourceInfo, IFacadesFactory facadesFactory,User user)
        {
            _dataSourceInfo = dataSourceInfo;
            _metaDataAdminFacade = facadesFactory.CreateMetaDataAdminFacade();
            _commandFacade = facadesFactory.CreateCommandFacade();
            _reactiveDataQueryFacade = facadesFactory.CreateReactiveDataQueryFacade();
            _temporalDataQueryFacade = facadesFactory.CreateTemporalDataQueryFacade();
            
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

            return _metaDataAdminFacade.GetWorksheetInfo(worksheet.DataSourcePath);
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
            return _reactiveDataQueryFacade.GetData(_dataSourceInfo.DataSourcePath, filterRules);
        }

        public IObservable<RecordChangedParam> GetDataChanges(FilterRule[] filterRules = null)
        {
            return _reactiveDataQueryFacade.GetDataChanges(_dataSourceInfo.DataSourcePath, filterRules);
        }

        public IEnumerable<Dictionary<string,object>> GetHistory(string recordKey)
        {
            return _temporalDataQueryFacade.GetRecordsHistory(_dataSourceInfo, recordKey);
        }

        public DataSourceInfo UpdateDataSourceInfo(DataSourceInfo dataSourceInfo,User user)
        {
            _metaDataAdminFacade.UpdateDataSourceInfo(dataSourceInfo,_dataSourceInfo.DataSourcePath,user.Id);
            _dataSourceInfo = _metaDataAdminFacade.GetDataSourceInfo(dataSourceInfo.DataSourcePath);
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