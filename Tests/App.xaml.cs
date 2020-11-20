using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AddTranslation.Windows;
namespace Tests
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private void Application_Startup(object sender, StartupEventArgs e)
    {
      TestClass.RunTests();
      (new MainWindow("przykładowy tekst",
        @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp\ConsoleApp\ConsoleApp.csproj", out bool _)).ShowDialog();
    }
  }
}
