using System;
using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class TagInfo
    {
        public string TagName { get; set; }

        public string DataSourceProviderString { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Comment { get; set; }

        public string UserId { get; set; }

        public List<string> Revisions { get; set; }
    }
}
