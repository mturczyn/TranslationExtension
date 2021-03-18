using AddTranslationCore;
using System.Windows;

namespace AddTranslationTestApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var projFactory = new ProjectsFactoryFromSolutionFile();
            var vm = new AddTranslationViewModel(projFactory);
            var wnd = new MainWindow();
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }
    }
}
