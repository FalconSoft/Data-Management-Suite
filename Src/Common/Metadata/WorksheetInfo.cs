using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class WorksheetInfo : HeaderInfo
    {
        public DataSourceInfo DataSourceInfo { get; set; }

        public List<ColumnInfo> Columns { get; set; }

        public string FilterString { get; set; }

        public bool IsVisibleGroupPanel { get; set; }

        public bool IsVisibleAutoFilter { get; set; }

        public bool IsVisibleCount { get; set; }

        public bool IsVisibleTotalSummary { get; set; }
    }
}
