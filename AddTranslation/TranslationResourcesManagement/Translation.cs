namespace AddTranslation.TranslationResourcesManagement
{
  public class Translation
  {
    public string Name { get; set; }
    public string PolishText { get; set; }
    public string EnglishText { get; set; }

    public Translation(string name, string polishText, string englishText)
    {
      Name = name;
      PolishText = polishText;
      EnglishText = englishText;
    }

    public override string ToString()
    {
      return $"Nazwa: {Name}, polski: {PolishText}, angielski: {EnglishText}";
    }
  }
}
