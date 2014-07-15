using System.Runtime.InteropServices;
using ExcelDna.Integration.CustomUI;
using FalconSoft.ExcelAddIn.Views;

namespace FalconSoft.ExcelAddIn.Utils
{
    [ComVisible(true)]
    public class CustomRibbon : ExcelRibbon
    {
        public static void OpenWorksheet()
        {
            UserForm<WorksheetWindow>.ShowUserForm();
        }

        public static void OpenDesigner()
        {
            UserForm<WorksheetDesignerWindow>.ShowUserForm();
        }
    }
}
