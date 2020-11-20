# zamiana plików XAML
$sourcePath = '..\\..\\..\\AddTranslation\\Windows\\AddTranslationWindow.xaml';
$content = Get-Content $sourcePath -Raw -Encoding "UTF8";
$indexOfGrid = $content.IndexOf("<Grid>");
$indexOfClosingGrid = $content.LastIndexOf("</Grid>");
$content = $content.Substring($indexOfGrid, $indexOfClosingGrid - $indexOfGrid + "</Grid>".Length);

$destPath = '..\\..\\MainWindow.xaml';
$contentToReplace = Get-Content $destPath -Raw -Encoding "UTF8";
$indexOfGrid = $contentToReplace.IndexOf("<Grid>");
$indexOfClosingGrid = $contentToReplace.LastIndexOf("</Grid>");
$contentToReplace = $contentToReplace.Remove($indexOfGrid, $indexOfClosingGrid - $indexOfGrid + "</Grid>".Length);
$contentToReplace = $contentToReplace.Insert($indexOfGrid, $content);

Set-Content $destPath $contentToReplace -Encoding "UTF8";

# Zamiana kodu źródłowego
$sourcePath = '..\\..\\..\\AddTranslation\\Windows\\AddTranslationWindow.xaml.cs';
$content = Get-Content $sourcePath -Raw -Encoding "UTF8";
$indexOfGrid = $content.IndexOf("{", $content.IndexOf("{") + 1);
$indexOfClosingGrid = $content.LastIndexOf("}");
$content = $content.Substring($indexOfGrid, $indexOfClosingGrid - $indexOfGrid + 1);

$destPath = '..\\..\\MainWindow.xaml.cs';
$contentToReplace = Get-Content $destPath -Raw -Encoding "UTF8";
$indexOfGrid = $contentToReplace.IndexOf("{", $contentToReplace.IndexOf("{") + 2);
$indexOfClosingGrid = $contentToReplace.LastIndexOf("}");
$contentToReplace = $contentToReplace.Remove($indexOfGrid, $indexOfClosingGrid - $indexOfGrid + 1);
$contentToReplace = $contentToReplace.Insert($indexOfGrid, $content).Replace("AddTranslationWindow", "MainWindow");

Set-Content $destPath $contentToReplace -Encoding "UTF8";