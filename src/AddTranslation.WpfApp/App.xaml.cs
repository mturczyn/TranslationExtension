using AddTranslation.Core;
using System.Windows;

namespace AddTranslation.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var wnd = new MainWindow();
            var projFactory = new ProjectsFactoryFromSolutionFile();
            var vm = new AddTranslationViewModel(projFactory, wnd);
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }
    }
}
