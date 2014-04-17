using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common.Facade
{
    public interface ITemporalDataQueryFacade
    {
        IEnumerable<Dictionary<string,object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey);

        IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo);

        IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp);

        IEnumerable<TagInfo> GeTagInfos();

        void SaveTagInfo(TagInfo tagInfo);

        void RemoveTagInfo(TagInfo tagInfo);
    }
}
