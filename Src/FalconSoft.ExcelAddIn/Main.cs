using ExcelDna.Integration;
using FalconSoft.ExcelAddIn.Utils;
using FalconSoft.ExcelAddIn.Views;

namespace FalconSoft.ExcelAddIn
{
    public class Main
    {
        [ExcelCommand]//(Description = "Test Open WS", MenuName = "TestOpen Ws Name", MenuText = "TestOpen Ws Text")]
        public static void OpenWorksheet()
        {
            UserForm<WorksheetWindow>.ShowUserForm();
        }

        [ExcelCommand]//(Description = "Test Open Designer", MenuName = "TestOpen Des Name", MenuText = "TestOpen Des Text")]
        public static void OpenDesigner()
        {
            UserForm<WorksheetDesignerWindow>.ShowUserForm();
        }
    }
}
