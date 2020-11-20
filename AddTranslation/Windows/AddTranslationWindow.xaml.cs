using AddTranslation.LogService;
using AddTranslation.TranslationResourcesManagement;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AddTranslation.Windows
{
  /// <summary>
  /// Interaction logic for UserControl1.xaml
  /// </summary>
  public partial class AddTranslationWindow : BaseDialogWindow
  {
    private string _translation;
    private string _toTransalte;
    private string _translationName;
    private TranslationResourcesManager _translationResourcesManager;
    public string Translation
    {
      get
      {
        return tbTranslation.Text;
      }
    }

    public string TextToTranslate
    {
      get
      {
        return tbTextToTranslate.Text;
      }
    }

    public string TranslationName
    {
      get
      {
        return _translationResourcesManager.Namespace + '.' +
          _translationResourcesManager.DesignerFileName + '.' +
          tbTranslationName.Text;
      }
    }

    public AddTranslationWindow(string text, string csProjPath, out bool shouldNotOpenTheWindow)
    {
      Logger.AppendInfoLine($"Wywołanie okna do tłumaczeń z tekstem {text ?? "NULL"} oraz ścieżką" +
        $" {csProjPath ?? "NULL"}");

      InitializeComponent();
      tbTextToTranslate.Text = text;
      _toTransalte = text;
      _translation = "";
      _translationName = "";
      // Jeśli zmodyfikowany zostanie plik csproj lub nie dostaniemy żadnych tłumaczeń, to nie otwieramy w ogóle okna.
      _translationResourcesManager = TranslationResourcesManager.GetInstance(csProjPath, out shouldNotOpenTheWindow);
      shouldNotOpenTheWindow = shouldNotOpenTheWindow || ! GetSortedTranslations(text);
    }


    private void ChangeResourcesPath_Click(object sender, RoutedEventArgs e)
    {
      _translationResourcesManager.ChangeResourcePath();
      dgPossibleTranslations.ItemsSource = null;
    }
    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(tbTranslationName?.Text))
      {
        MessageBox.Show("Wprowadź nazwę tłumaczenia!", "Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      bool success = _translationResourcesManager.TrySaveTranslation(tbTranslationName.Text, tbTextToTranslate.Text, tbTranslation.Text, _updatingTranslation, _translationName);
      if (success)
        Close(true);
      else
        return;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
      Close(false);
    }

    private void SaveLogs_Click(object sender, RoutedEventArgs e)
    {
      Logger.SaveLogs();
    }

    public void Close(bool result)
    {
      DialogResult = result;
      Close();
    }

    private void tbTextToTranslate_TextChanged(object sender, TextChangedEventArgs e)
    {
      ChangeRespectiveTextToUnsaved(lblTextToTranslate, _toTransalte, tbTextToTranslate?.Text);
    }

    private void tbTranslationName_TextChanged(object sender, TextChangedEventArgs e)
    {
      ChangeRespectiveTextToUnsaved(lblTranslationName, _translationName, tbTranslationName?.Text);
    }

    private void tbTranslation_TextChanged(object sender, TextChangedEventArgs e)
    {
      ChangeRespectiveTextToUnsaved(lblTranslation, _translation, tbTranslation?.Text);
    }

    private void BtnUseTranslation_Click(object sender, RoutedEventArgs e)
    {
      UseTranslation();
    }

    private void DgPossibleTranslations_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      UseTranslation();
    }

    #region Metody pomocnicze
    /// <summary>
    /// Pobiera i sortuje tłumaczenia.
    /// </summary>
    /// <param name="stringToSortBy"></param>
    /// <returns>Jesli nie uda się pobrać żadnych tłumaczeń (lita == null) to zwracamy false.</returns>
    private bool GetSortedTranslations(string stringToSortBy)
    {
      var possibleTranslations = _translationResourcesManager.GetAllSimilairTranslations(stringToSortBy);
      dgPossibleTranslations.ItemsSource = null;
      if (possibleTranslations == null)
        return false;
      dgPossibleTranslations.ItemsSource = possibleTranslations;
      return true;
    }
    private void ChangeRespectiveTextToUnsaved(Label label, string originalText, string currentText)
    {
      if (label == null || originalText == null || currentText == null)
        return;

      if (originalText != currentText)
      {
        if (!label.Content.ToString().EndsWith("*"))
          label.Content += "*";
      }
      else
        label.Content = label.Content.ToString().TrimEnd('*');
    }

    private void BtnSearchPhrase_Click(object sender, RoutedEventArgs e)
    {
      GetSortedTranslations(tbPhraseToSearch.Text);
    }
    private bool _updatingTranslation = false;
    private void UseTranslation()
    {
      if (dgPossibleTranslations.SelectedIndex == -1)
        return;
      var translation = dgPossibleTranslations.SelectedItem as Translation;
      if (translation == null)
        return;

      _updatingTranslation = true;

      Logger.AppendInfoLine($"Wybrano tłumaczenie do modyfikacji: {translation.Name}");

      tbTranslationName.Text = translation.Name;
      _translationName = translation.Name;
      tbTextToTranslate.Text = translation.PolishText;
      _toTransalte = translation.PolishText;
      tbTranslation.Text = translation.EnglishText;
      _translation = translation.EnglishText;
      // Zmieniamy na zieolny kolor kontrolki, bo teraz mamy sytuację, ze nic się nie zmieniło
      ChangeRespectiveTextToUnsaved(lblTextToTranslate, "", "");
      ChangeRespectiveTextToUnsaved(lblTranslation, "", "");
      ChangeRespectiveTextToUnsaved(lblTranslationName, "", "");
    }
    #endregion
  }
}
