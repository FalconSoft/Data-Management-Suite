using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Facade;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class AggregatedWorksheetInfo : HeaderInfo
    {
        public DataSourceInfo DataSourceInfo { get; set; }

        public List<ColumnInfo> GroupByColumns { get; set; }

        public List<KeyValuePair<AggregatedFunction, ColumnInfo>> Columns { get; set; }
    }
}
