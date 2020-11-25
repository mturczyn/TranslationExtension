using System.Windows;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Languages.Resource.Culture = new System.Globalization.CultureInfo("en-US");
            new MainWindow().Show();
        }
    }
}
