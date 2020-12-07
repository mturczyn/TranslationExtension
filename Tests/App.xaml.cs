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


            var vm = new AddTranslationUI.AddTranslationViewModel();
            vm.ProjectReferences.Add(new ProjectItem() { ProjectName = "Project name" });
            var wnd = new MainWindow();
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }
    }
}
