using System.Threading;
using System.Windows;

namespace FalconSoft.ExcelAddIn.Utils
{
    public class UserForm<T> where T : Window, new()
    {
        private static Thread _thread;
        public static void ShowUserForm()
        {
            _thread = new Thread(DoWork);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private static void DoWork()
        {
            var win = new T();
            win.Show();
            win.Closed += (sender1, e1) => win.Dispatcher.InvokeShutdown();
            System.Windows.Threading.Dispatcher.Run();
        }
    }
}
