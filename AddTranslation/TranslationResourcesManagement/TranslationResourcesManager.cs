using AddTranslation.LogService;
using ClosestStringFinding;
using EnvDTE;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace AddTranslation.TranslationResourcesManagement
{
  // Klasa, która jest odpowiedzialna za interakcję z zasobami:
  // - walidacja, dodawanie wyszukiwanie tłumaczeń
  // - walidacja ścieżek i ich dodawanie do pliku csproj
  public class TranslationResourcesManager
  {
    // instancja signletona
    private static TranslationResourcesManager _instance = null;

    // Używane, aby nie informować wiele razy o różnicach w pliku, jeśli zostały zaakceptowane.
    private bool _userAcceptedDifferences = false;
    // Ścieżka do domyślnego (polskiego) pliku zasobów
    private string _defaultResPath;
    // Ścieżka do angielskiego pliku zasobów
    private string _englishResPath;
    private string _designerPath;

    private static string _csProjPath;

    public static TranslationResourcesManager GetInstance(string csProjPath, out bool csProjFileModified)
    {
      csProjFileModified = false;
      // Zawsze chcemy załadować od nowa plik, bo może ulec zmianie podczas pracy z Visualem, a zatem w pamięci możemy
      // mieć plik zasobów, na który już nie wskazuje ścieżka w pliku csproj.
      //if (_instance == null || csProjPath != _csProjPath)
      _instance = new TranslationResourcesManager(csProjPath, out csProjFileModified);
      return _instance;
    }

    public string Namespace { get; private set; } = "";
    public string DesignerFileName
    {
      get
      {
        return _designerPath.Substring(_designerPath.LastIndexOf('\\') + 1).Replace(".Designer.cs", "");
      }
    }

    private TranslationResourcesManager(string csProjPath, out bool csProjFileModified)
    {
      csProjFileModified = false;
      Logger.AppendInfoLine($"Tworzenie menadżera zasobów tłumaczeń ze ścieżką {csProjPath ?? "NULL"}");
      _csProjPath = csProjPath;
      if (!ValidateCsProjFile(out csProjFileModified))
      {
        MessageBox.Show("Nie powiodło się wczytywanie pliku .csrpoj projektu!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
    }

    public void ChangeResourcePath()
    {
      if (!(
        LoadDefaultResourcesFile() &&
        ValidateEnglishResource() &&
        ValidateDesignerFile()) 
      ) return;

      try
      {
        var csProjXml = GetCsProjFile(out XElement pathNode, out bool csProjFileModified);
        pathNode.Value = GetRelativePath(_defaultResPath, _csProjPath);
        csProjXml.Save(_csProjPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Nie powiodła się zmiana ścieżki.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        Logger.AppendErrorLine(ex.ToString());
      }
    }

    private bool LoadDefaultResourcesFile()
    {
      try
      {
        var ofd = new OpenFileDialog();
        ofd.Title = "Wybierz polski (domyślny) zasób";
        ofd.Filter = "Resource file (*.resx)|*.resx";
        ofd.Multiselect = false;
        if (ofd.ShowDialog() ?? false)
        {
          Logger.AppendInfoLine($"Wybrano plik z polskimi zasobami: {ofd.FileName}");
          _defaultResPath = ofd.FileName;
        }
        else
        {
          Logger.AppendErrorLine("Nie wybrano żadnego pliku z polskimi zasobami!");
          MessageBox.Show("Nie wybrano pliku zasobów! Dodawanie tłumaczeń nie będzie działać poprawnie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
          return false;
        }
      }
      catch
      {
        Logger.AppendErrorLine("Nie powiódł się wybór pliku zasobów.");
        MessageBox.Show("Nie powiódł się wybór pliku zasobów. Dodawanie tłumaczeń nie będzie działać poprawnie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Zapisuje ustawienie o kluczu podanym w parametrze name. Zwraca true, jeśli udało sie zapisać lub false
    /// jesli nie udało się.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="polishText"></param>
    /// <param name="englishText"></param>
    /// <returns></returns>
    public bool TrySaveTranslation(string name, string polishText,
      string englishText, bool updatingTranslation = false, string oldName = null)
    {
      (var polishResource, var englishResource) = LoadAndValidateResources();
      if (polishResource == null || englishResource == null)
        return false;
      Logger.AppendInfoLine("Próba zapisu tłumaczeń z parametrami: " +
        $"name={name}, polishText={polishText}, englishText={englishText}, oldName={oldName ?? "NUL"}");

      var nameToSearchBy = oldName ?? name;

      var nodeWithPolishTranslation = GetNode(polishResource, nameToSearchBy);
      var nodeWithEnglishTranslation = GetNode(englishResource, nameToSearchBy);

      bool designerUpdated = false;
      try
      {
        if (updatingTranslation)
        {
          if (MessageBox.Show($"Czy na pewno chcesz nadpisać ustawienie {nameToSearchBy}?", "Tłumaczenie istnieje", MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes)
          {
            UpdateTranslationNode(nodeWithPolishTranslation, name, polishText);
            UpdateTranslationNode(nodeWithEnglishTranslation, name, englishText);
            designerUpdated = UpdateTranslationInDesigner(name, polishText, oldName);
          }
          else
          {
            return false;
          }
        }
        else
        {
          // Sprawdzamy, czy istnieje takie tłumaczenie już w słowniku.
          var polishTranslation = GetNode(polishResource, name);
          if (polishTranslation != null)
          {
            MessageBox.Show("Tłumaczenie z tą nazwą już istnieje, wybierz inna nazwę");
            return false;
          }

          var polishNode = CreateNodeWithTranslation(name, polishText);
          var englishNode = CreateNodeWithTranslation(name, englishText);

          AddTranslationNode(polishResource, polishNode);
          AddTranslationNode(englishResource, englishNode);
          designerUpdated = AddTranslationInDesigner(name, polishText);
        }
        polishResource.Save(_defaultResPath);
        englishResource.Save(_englishResPath);

        return designerUpdated;
      }
      catch (Exception ex)
      {
        var error = $"Nie powiódł się zapis tłumaczeń!\n{ex.ToString()}";
        Logger.AppendErrorLine(error);
        MessageBox.Show(error);
        return false;
      }
    }

    private XElement GetNode(XDocument resource, string name)
    {
      var node = resource.Descendants()
        .Where(n => n.Name == "data" && n.Attribute("name").Value == name)
        .FirstOrDefault();
      if (node != null)
        Logger.AppendInfoLine($"Pobrano węzeł {name} z tłumaczeniem {node.Descendants("value").FirstOrDefault()?.Value ?? "NULL"}");
      else
        Logger.AppendInfoLine($"Nie udalo się pobrać węzła o nazwie {name}, dodawanie nowego węzła");
      return node;
    }

    private XElement CreateNodeWithTranslation(string name, string content)
    {
      Logger.AppendInfoLine($"Tworzenie nowego węzła: name={name}, content={content}");

      XElement node = new XElement("data", new XElement("value", content));
      node.SetAttributeValue("name", name);
      node.SetAttributeValue(XNamespace.Xml + "space", "preserve");

      Logger.AppendInfoLine("Powiodło sie utworzenie nowego węzła");
      return node;
    }

    /// <summary>
    /// Dodaje węzeł z tłumaczeniem na samym końcu dokumentu, jako ostatnie dziecko,
    /// ponieważ na chwilę obecną tak są skonstruowane nasze resource'y.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="nodeToAdd"></param>
    private void AddTranslationNode(XDocument resource, XElement nodeToAdd)
    {
      Logger.AppendInfoLine($"Próba dodania nowego węzła: {nodeToAdd.Attribute("name")?.Value ?? "NULL"}");
      var lastNode = resource.Descendants("data").LastOrDefault();
      // Jeśli nie mieliśmy żadnych węzłow data, to szukamy resheader, który powinien być i po nim dodamy nasz węzeł
      if(lastNode == null)
        lastNode = resource.Descendants("resheader").LastOrDefault();
      if(lastNode == null)
      {
        Logger.AppendWarningLine("Nie znaleziono żadnych węzłów, po których można dodać węzeł z tłumaczeniem!");
        return;
      }
      lastNode.AddAfterSelf(nodeToAdd);
      Logger.AppendInfoLine("Dodanie węzła powiodło się");
    }

    private void UpdateTranslationNode(XElement nodeToUpdate, string name, string content)
    {
      Logger.AppendInfoLine($"Próba aktualizacji węzła: {nodeToUpdate.Attribute("name")?.Value ?? "NULL"} z parametrami: " +
        $"name={name}, content={content}");
      var node = nodeToUpdate.Descendants("value").FirstOrDefault();
      if(node != null) 
        node.Value = content;
      nodeToUpdate.Attribute("name").Value = name;
      Logger.AppendInfoLine("Aktualizacja węzła powiodła się");
    }

    private List<Translation> GetTranslations(XDocument polishRes, XDocument englishRes)
    {
      Logger.AppendInfoLine("Próba pobrania wszystkich tłumaczeń");

      var polishTranslations = polishRes.Descendants()
        .Where(n => n.Name == "data")
        .Select(n => (Name: n.Attribute("name").Value, Text: n.Element("value").Value)).ToArray();

      var englishTranslations = englishRes.Descendants()
        .Where(n => n.Name == "data")
        .Select(n => (Name: n.Attribute("name").Value, Text: n.Element("value").Value)).ToArray();

      var translations = new List<Translation>();

      var missingEngTranslations = new List<string>();

      for (int i = 0; i < polishTranslations.Length; i++)
      {
        var name = polishTranslations[i].Name;
        var engTxt = englishTranslations.Where(tr => tr.Name == name).FirstOrDefault();
        if (engTxt == (null, null))
        {
          missingEngTranslations.Add(name);
        }
        translations.Add(new Translation(
          name,
          polishTranslations[i].Text,
          engTxt.Text ?? ""
        ));
      }

      // My już przy włączeniu okienka informujemy o niezgodnych kluczach, więc tutaj już tego nie musimy robić.
      //if (missingEngTranslations.Count > 0)
      //{
      //  MessageBox.Show($"Tłumaczenia o kluczach:\n{string.Join(", ", missingEngTranslations)}\nnie istineją w angielskim słowniku", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
      //  Logger.AppendWarningLine($"Tłumaczenia o kluczach:\n{string.Join(", ", missingEngTranslations)}\nnie istineją w angielskim słowniku");
      //}

      Logger.AppendInfoLine("Pomyślnie pobrano wszystkie tłuamczenia");
      return translations;
    }

    /// <summary>
    /// Ładuje zasoby z plików, zapisuje do zmiennych prywatnych XMLe, następnie
    /// je zwraca jako słowiniki nazwa - tłumaczenie. Jak coś się nie uda zwraca nulle!
    /// Należy wywołać przed każdą operacją na zasobach.
    /// </summary>
    /// <returns></returns>
    private  (XDocument polish, XDocument english) LoadAndValidateResources()
    {
      Logger.AppendInfoLine("Próba załadowania plików z zasobami");
      if (_defaultResPath == null) return (null, null);
      try
      {
        var polishResource = XDocument.Load(_defaultResPath);
        var englishResource = XDocument.Load(_englishResPath);

        var polishKeys = polishResource.Descendants().Where(n => n.Name == "data").Select(n => n.Attribute("name").Value).ToArray();
        var englishKeys = englishResource.Descendants().Where(n => n.Name == "data").Select(n => n.Attribute("name").Value).ToArray();
        // Sprawdzamy czy klucze w obu słownikach sie zgadzają i są unikatowe
        if(!_userAcceptedDifferences &&
          (polishKeys.Length != englishKeys.Length || 
          polishKeys.Length != polishKeys.Distinct().Count() ||
          polishKeys.Except(englishKeys).Count() != 0))
        {
          var error = "Klucze w słownikach się nie zgadzają. Należy to poprawić i uruchomić dokument ponownie. Niezgodne klucze:\n" +
            string.Join(", ", polishKeys.Except(englishKeys)) + "\n" +
            string.Join(", ", englishKeys.Except(polishKeys))
            + "\nKontnuować pomimo to?";
          Logger.AppendErrorLine(error);

          _userAcceptedDifferences = MessageBox.Show(error, "Błąd", MessageBoxButton.YesNo, MessageBoxImage.Error)
            == MessageBoxResult.Yes;
          if(!_userAcceptedDifferences)
            return (null, null);
        }

        return (polishResource, englishResource);
      }
      catch (Exception ex)
      {
        var error = "Nie udało się załadować zasobów. Upewnij się, że zasoby są w tym samym katalogu" +
          $" i uruchom ponownie plik!\nBłąd: {ex.ToString()}";
        Logger.AppendErrorLine(error);
        MessageBox.Show(error, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        return (null, null);
      }
    }

    /// <summary>
    /// Wyszukuje podobne tłumaczenia zawierające podany tekst zawierający wszystkie podane słowa
    /// (niekoniecznie w danej kolejności). Wyszukuwianie jest case insensitive.
    /// </summary>
    /// <param name="textToFind"></param>
    /// <returns></returns>
    public List<Translation> GetAllSimilairTranslations(string textToFind)
    {
      Logger.AppendInfoLine($"Próba znalezienia tłumaczeń podobnych do: {textToFind}");

      (var polishResource, var englishResource) = LoadAndValidateResources();
      if (polishResource == null || englishResource == null)
        return null;

      var translations = GetTranslations(polishResource, englishResource);

      return string.IsNullOrEmpty(textToFind) ? translations :
        translations.OrderBy(tr => ClosestStringMatch.GetDistance(textToFind, tr.PolishText)).ToList();
    }

    #region Metody do obsługi pliku designera

    private string _commentPattern = "Looks up a localized string similar to ";
    private string _declarationPattern = "public static string {0} ";

    private string GetNamespace(string text, out bool isFound)
    {
      if (!text.Trim().StartsWith("namespace"))
      {
        isFound = false;
        return "";
      }
      isFound = true;
      return text.Replace("namespace", "").Replace("{", "").Trim();
    }

    private bool UpdateTranslationInDesigner(string name, string text, string oldName = null)
    {
      Logger.AppendInfoLine("Rozpoczęcie metody UpdateTranslationInDesigner z parametrami: " +
        $"name={name}, text={text}, oldName={oldName ?? "NULL"}");

      var nameToSearchBy = oldName ?? name;
      var tempDesignerPath = _designerPath.Insert(_designerPath.LastIndexOf('\\') + 1, "Temp");

      Logger.AppendInfoLine($"Ściezka tymczasowego zasobu: {tempDesignerPath}");

      StreamReader sr = null;
      StreamWriter sw = null;

      try
      {
        sr = new StreamReader(_designerPath);
        sw = new StreamWriter(tempDesignerPath);
        // Chcemy mieć dwie linijki do przodu przeczytane, aby więdzieć kiedy trafimy na pole,
        // który chcemy nadpisać (właściwośc w klasie), wówczas łątwo zastąpimy cały blok wraz z
        // komentarzem. Spokojnie możemy już przepisać pierwszą linijkę.
        var currentLine = sr.ReadLine();
        sw.WriteLine(currentLine);
        var lineAhead = sr.ReadLine();
        var twoLinesAhead = sr.ReadLine();
        Logger.AppendInfoLine("Rozpoczęto przpeisywanie pliku designera.");

        var namespaceFound = false;
        while (! sr.EndOfStream)
        {
          currentLine = lineAhead;
          lineAhead = twoLinesAhead;
          twoLinesAhead = sr.ReadLine();
          if (!namespaceFound)
            Namespace = GetNamespace(currentLine, out namespaceFound);
          // Jeśli trafimy na deklarację właściwości, którą chcemy zaktualizować...
          if (twoLinesAhead.Contains(string.Format(_declarationPattern, nameToSearchBy)))
          {
            Logger.AppendInfoLine($"Znaleziono deklarację szukanej właściwości: {nameToSearchBy}");
            Logger.AppendInfoLine(twoLinesAhead);
            // ... to currentLine (obecna linia) jest komentarzem, więc sklejamy własny komentarz,
            // tak aby wcięcia się zgadzały (taby), oraz dwie następne
            currentLine = currentLine.Substring(0, currentLine.IndexOf(_commentPattern) + _commentPattern.Length) + text;
            sw.WriteLine(currentLine);
            sw.WriteLine(lineAhead);
            sw.WriteLine(twoLinesAhead.Replace(nameToSearchBy, name));
            sw.WriteLine(sr.ReadLine());
            sw.WriteLine(sr.ReadLine().Replace(nameToSearchBy, name));

            Logger.AppendInfoLine("Pomyślnie zaktualziowano właściwość w designerze.");
            // Znaleźliśmy, co mieliśmy, więc kończymy tę pętlę i resztę pliku przepisujemy normalnie
            break;
          }
          else
            sw.WriteLine(currentLine);
        }
        Logger.AppendInfoLine("Przpeisywanie reszty pliku...");
        while (! sr.EndOfStream)
          sw.WriteLine(sr.ReadLine());
        Logger.AppendInfoLine("Przepisano plik designera");
        // Zamykamy strumienie i zastępujemy plik designera
        sr.Close();
        sw.Close();
        File.Replace(tempDesignerPath, _designerPath, null);
        Logger.AppendInfoLine("Zastąpiono plik designera tymczasowym, zaktualizowanym plikiem.");
        return true;
      }
      catch(Exception ex)
      {
        var error = $"Nie powiódł się zapis do designera! Błąd: {ex.ToString()}";
        Logger.AppendErrorLine(error);
        MessageBox.Show(error);

        return false;
      }
      finally
      {
        if (sr != null)
          sr.Dispose();
        if (sw != null)
          sw.Dispose();
      }
    }
    /// <summary>
    /// Dodaje tłumaczenie do słownika.
    /// </summary>
    /// <param name="name">Nazwa tłumaczenia</param>
    /// <param name="text">Tłumaczenie</param>
    /// <returns>Czy powiodło się dodawanie tłumaczenia (czyli też czy plik został zmodyfikowany)/</returns>
    private bool AddTranslationInDesigner(string name, string text)
    {
      Logger.AppendInfoLine("Rozpoczęcie metody AddTranslationInDesigner z parametrami: " +
        $"name={name}, text={text}");

      var tempDesignerPath = _designerPath.Insert(_designerPath.LastIndexOf('\\') + 1, "Temp");

      Logger.AppendInfoLine($"Ściezka tymczasowego zasobu: {tempDesignerPath}");
      StreamReader sr = null;
      StreamWriter sw = null;

      try
      {
        sr = new StreamReader(_designerPath);
        sw = new StreamWriter(tempDesignerPath);
        // Chcemy mieć dwie linijki do przodu przeczytane, aby więdzieć kiedy trafimy koniec pliku.
        var currentLine = sr.ReadLine();
        sw.WriteLine(currentLine);
        var lineAhead = sr.ReadLine();
        var twoLinesAhead = sr.ReadLine();

        var namespaceFound = false;
        while (! sr.EndOfStream)
        {
          if (!namespaceFound)
            Namespace = GetNamespace(currentLine, out namespaceFound);

          currentLine = lineAhead;
          lineAhead = twoLinesAhead;
          twoLinesAhead = sr.ReadLine();
          sw.WriteLine(currentLine);
        }
        Logger.AppendInfoLine("Zakończono odczytywanie pliku designera. Dodawanie właściwości z tłumaczeniem.");
        // Dodajemy własciwość z naszym tłumaczeniem na końcu klasy
        sw.WriteLine("\t\t/// <summary>");
        sw.WriteLine("\t\t/// \t" + _commentPattern + text);
        sw.WriteLine("\t\t/// </summary>");
        sw.WriteLine("\t\t" + string.Format(_declarationPattern, name) + "{");
        sw.WriteLine("\t\t\tget {");
        sw.WriteLine($"\t\t\t\treturn ResourceManager.GetString(\"{name}\", resourceCulture);");
        sw.WriteLine("\t\t\t}");
        sw.WriteLine("\t\t}");
        sw.WriteLine("\t}");
        sw.WriteLine("}");
        Logger.AppendInfoLine("Zakończono zapis pliku designera.");
        // Zamykamy strumienie i zastępujemy plik designera
        sr.Close();
        sw.Close();
        File.Replace(tempDesignerPath, _designerPath, null);
        Logger.AppendInfoLine("Zastąpiono plik designera tymczasowym, zaktualizowanym plikiem.");
        return true;
      }
      catch (Exception ex)
      {
        var error = $"Nie powiódł się zapis do designera! Błąd: {ex.ToString()}";
        Logger.AppendErrorLine(error);
        MessageBox.Show(error);

        return false;
      }
      finally
      {
        if (sr != null)
          sr.Dispose();
        if (sw != null)
          sw.Dispose();
      }
    }
    #endregion
    #region Obsługa pliku konfiguracyjnego
    private XDocument GetCsProjFile(out XElement pathNode, out bool csProjFileModified)
    {
      var propertyGroupNodeName = "PropertyGroup";
      var pathNodeName = "Path";
      var labelAttributeName = "Label";
      var labelAttributeValue = "Translation";
      csProjFileModified = false;

      var csProjXml = XDocument.Load(_csProjPath);
      var ns = csProjXml.Root.GetDefaultNamespace();
      var validatedNs = string.IsNullOrEmpty(ns.ToString()) ? "" : "{" + ns + "}";
      var translationPropGroup = csProjXml.Root.Descendants(validatedNs + propertyGroupNodeName)
        .Where(e => e.Attribute(labelAttributeName)?.Value == labelAttributeValue)
        .FirstOrDefault();
      if (translationPropGroup == null)
      {
        translationPropGroup = new XElement(validatedNs + propertyGroupNodeName);
        translationPropGroup.SetAttributeValue(labelAttributeName, labelAttributeValue);
        csProjXml.Root.Add(translationPropGroup);

        csProjFileModified = true;
      }
      Logger.AppendInfoLine($"Mamy węzeł {propertyGroupNodeName}.");

      pathNode = translationPropGroup.Descendants(validatedNs + pathNodeName).FirstOrDefault();
      if (pathNode == null)
      {
        // Jeśli jeszcze nie wczytaliśmy pliku z zasobami, to je wczytaj
        if(_defaultResPath == null)
          if (!LoadDefaultResourcesFile()) return null;

        pathNode = new XElement(validatedNs + pathNodeName);
        pathNode.Value = GetRelativePath(_defaultResPath, _csProjPath); 
        translationPropGroup.Add(pathNode);

        csProjFileModified = true;
      }
      Logger.AppendInfoLine($"Mamy węzeł {pathNodeName}.");

      return csProjXml;
    }
    public bool ValidateCsProjFile(out bool csProjModified)
    {
      csProjModified = false;
      // Scieżka nie powinna być nullem i powinna byc poprawna,
      // bo już przed tworzeniem okna w rozszerzeniu to weryfikujemy
      if (!File.Exists(_csProjPath))
      {
        MessageBox.Show("Projekt nie posiada pliku konfiguracyjnego!");
        return false;
      }

      try
      {
        var csProjXml = GetCsProjFile(out XElement pathNode, out bool csProjFileModified);
        if (csProjXml == null) return false;
        _defaultResPath = Path.Combine(_csProjPath, pathNode.Value);

        if (!File.Exists(_defaultResPath))
        {
          MessageBox.Show($"Plik z polskimi zasobami {pathNode.Value} nie istnieje! Wybierz poprawny.");
          if (!LoadDefaultResourcesFile()) return false;

          pathNode.Value = GetRelativePath(_csProjPath, _defaultResPath);
          csProjFileModified = true;
        }

        if (!
          (ValidateEnglishResource() &&
          ValidateDesignerFile())
        ) return false;

        if (csProjFileModified)
        {
          csProjModified = csProjFileModified;
          Task.Factory.StartNew(() =>
            csProjXml.Save(_csProjPath)
          );
        }

        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show("Nie udało się zapisywanie ścieżki tłumaczen w pliku konfiguracyjnym.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        Logger.AppendErrorLine(ex.ToString());
        return false;
      }
    }

    private bool ValidateEnglishResource()
    {
      // Szukamy angielskiego zasobu
      string englishResFileName = _defaultResPath.Substring(_defaultResPath.LastIndexOf('\\') + 1);
      englishResFileName = englishResFileName.Insert(englishResFileName.LastIndexOf('.'), ".en");
      string fullPath = _defaultResPath.Substring(0, _defaultResPath.LastIndexOf('\\') + 1) + englishResFileName;
      if (!File.Exists(fullPath))
      {
        Logger.AppendWarningLine($"Spodziewany plik z angielskimi zasobami nie istnieje: {fullPath}");

        var ofd = new OpenFileDialog();
        ofd.Title = "Wybierz angielski zasób";
        ofd.Filter = "Resource file (*.resx)|*.resx";
        ofd.Multiselect = false;

        MessageBox.Show($"Nie udało się znaleźć angielskiego zasobu. Oczekiwano pliku w tym samym katalogu o nazwie {englishResFileName}", "Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
        ofd.Title = "Wybierz angielski zasób";
        if (ofd.ShowDialog() ?? false)
        {
          Logger.AppendInfoLine($"Wybrano plik z angielskimi zasobami: {ofd.FileName}");
          _englishResPath = ofd.FileName;
        }
        else
        {
          Logger.AppendErrorLine("Nie wybrano żadnego pliku z angielskimi zasobami!");
          MessageBox.Show("Nie wybrano pliku angielskiego zasobów! Dodawanie tłumaczeń nie będzie działać poprawnie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
          return false;
        }
      }
      else
        _englishResPath = fullPath;

      return true;
    }

    private bool ValidateDesignerFile()
    {
      // Szukamy pliku Designera
      string designerFileName = _defaultResPath.Substring(_defaultResPath.LastIndexOf('\\') + 1);
      designerFileName = designerFileName.Substring(0, designerFileName.LastIndexOf('.')) + ".Designer.cs";
      string fullPath = _defaultResPath.Substring(0, _defaultResPath.LastIndexOf('\\') + 1) + designerFileName;

      if (!File.Exists(fullPath))
      {
        var ofd = new OpenFileDialog();
        ofd.Title = "Wybierz angielski zasób";
        ofd.Filter = "Resource file (*.resx)|*.resx";
        ofd.Multiselect = false;

        Logger.AppendWarningLine($"Spodziewany plik designera nie istnieje: {fullPath}");

        MessageBox.Show($"Nie udało się pliku designera! Oczekiwano pliku w tym samym katalogu o nazwie {designerFileName}", "Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
        ofd.Title = "Wybierz plik designera";
        if (ofd.ShowDialog() ?? false)
        {
          Logger.AppendInfoLine($"Wybrano plik designera: {ofd.FileName}");
          _englishResPath = ofd.FileName;
        }
        else
        {
          Logger.AppendErrorLine("Nie wybrano żadnego pliku designera!");
          MessageBox.Show("Nie wybrano pliku designera! Dodawanie tłumaczeń nie będzie działać poprawnie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
          return false;
        }
      }
      else
        _designerPath = fullPath;

      return true;
    }
    #endregion

    #region Metody pomocnicze
    public string GetRelativePath(string pathTo, string pathFrom)
    {
      Uri uriTo = new Uri(pathTo);
      // Folders must end in a slash
      if (!pathFrom.EndsWith(Path.DirectorySeparatorChar.ToString()))
      {
        pathFrom += Path.DirectorySeparatorChar;
      }
      Uri uriFrom = new Uri(pathFrom);
      return Uri.UnescapeDataString(uriFrom.MakeRelativeUri(uriTo).ToString().Replace('/', Path.DirectorySeparatorChar));
    }
    #endregion
  }
}
