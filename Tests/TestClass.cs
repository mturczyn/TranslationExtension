using AddTranslation.TranslationResourcesManagement;
using System.IO;
using ClosestStringFinding;

namespace Tests
{
  static public class TestClass
  {
    static public void RunTests()
    {
      //ResourceTranslationManagerTest();
    }

    private static void ResourceTranslationManagerTest()
    {
      string csprojPath = @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp\ConsoleApp\ConsoleApp.csproj";
      TranslationResourcesManager translationResourcesManager = TranslationResourcesManager.GetInstance(csprojPath, out bool _);

      var relPath = translationResourcesManager.GetRelativePath(@"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp",
        @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp");
      relPath = translationResourcesManager.GetRelativePath(@"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp",
        @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\Languages");
      relPath = translationResourcesManager.GetRelativePath(@"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\ConsoleApp\ConsoleApp.csproj",
        @"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\Languages\Languages.csproj");

      relPath = Path.Combine(@"C:\Users\Mi\Desktop\CSharp\Aplikacje testowe\Languages\Languages.csproj", relPath);

      var translations = translationResourcesManager.GetAllSimilairTranslations("witaj świecie");
      if (translations.Count == 0) throw new System.Exception("Nie znaleziono tłuamczeń");
      //translations = translationResourcesManager.GetAllSimilairTranslations("jakiśDziwnyTekst");
      //if (translations.Count > 0) throw new System.Exception("Znaleziono tłumaczenia, a nie powinno");

      translationResourcesManager.TrySaveTranslation("HelloWorld", "asd", "asd");
      translationResourcesManager.TrySaveTranslation("HelloWorld1", "asd1", "asd1", true, "HelloWorld");
      translationResourcesManager.TrySaveTranslation("HelloNewWorld", "asd", "asd");
    }

    private static void StringDistanceTests()
    {
      string str1, str2;
      str1 = "abcd efgh";
      str2 = "abcd efgh";
      var dist = ClosestStringMatch.GetDistance(str1, str2);
      str2 = "grasdcbue";
      dist = ClosestStringMatch.GetDistance(str1, str2);
      str2 = "abcdefgh";
      dist = ClosestStringMatch.GetDistance(str1, str2);
    }
  }
}
