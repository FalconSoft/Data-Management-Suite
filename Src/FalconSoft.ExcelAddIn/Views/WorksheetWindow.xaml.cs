using FalconSoft.ReactiveWorksheets.ViewModels;

namespace FalconSoft.ExcelAddIn.Views
{
    /// <summary>
    /// Interaction logic for WorksheetWindow.xaml
    /// </summary>
    public partial class WorksheetWindow
    {
        public WorksheetWindow()
        {
            InitializeComponent();
            DataContext = new WorksheetViewModel(null);
        }
    }
}
