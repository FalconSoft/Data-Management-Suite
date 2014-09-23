using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ExcelAddIn.ReactiveExcel;

namespace ReactiveWorksheets.ExcelEngine.Tests
{
    public class MockReactiveFunctions
    {
        public MockReactiveFunctions(IReactiveExcelEngine excelEngine)
        {
            RExcelEngine = excelEngine;
        }

        public IReactiveExcelEngine RExcelEngine;

        public  object FDSInfo()
        {
            var serverInfo =  RExcelEngine.GetServerInfo();
            return (object)string.Format("FalconSoft Data Server v{0}, hosted on {1}", serverInfo.Version, serverInfo.Url);
        }

        public object RDP(string dataSourceUrn, string primaryKey, string fieldName, string query = null)
        {
            if (string.IsNullOrEmpty(dataSourceUrn) || string.IsNullOrEmpty(primaryKey) ||
                string.IsNullOrEmpty(fieldName)) return "Invalid Input Parameters";
            RExcelEngine.RegisterSubject(dataSourceUrn, primaryKey, fieldName,null);
            return null;
        }
    }
}
