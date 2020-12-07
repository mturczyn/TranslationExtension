using System.Windows;

namespace Tests
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //TestClass.RunTests();
            //(new MainWindow("przykładowy tekst",
            //  @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp\ConsoleApp\ConsoleApp.csproj", out bool _)).ShowDialog();

            var projFactory = new ProjectsFactoryFromSolutionFile();
            var vm = new AddTranslationUI.AddTranslationViewModel(projFactory);
            var wnd = new MainWindow();
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }
    }
}
