using AddTranslationCore.Abstractions;
using AddTranslationCore.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AddTranslationCore.ViewModel
{
    public class Project : BaseObservable, IProjectItem
    {
        public event Action<string[]> DuplicatedKeysFound;
        /// <summary>
        /// This is placeholder used to hold place and allow easy insertion
        /// of necessary indent in a property.
        /// </summary>
        private const string _indentPlaceholder = "_I_N_D_E_N_T_";
        /// <summary>
        /// Formatable string holding pattern for field generated in designer file.
        /// </summary>
        private static string _translationDesignerPropertyFormat =
              $"{_indentPlaceholder}" + "/// <summary>" + 
            $"\n{_indentPlaceholder}" + "///     Looks up a localized string similar to {0}." + 
            $"\n{_indentPlaceholder}" + "/// </summary>" +
            $"\n{_indentPlaceholder}" + "public static string {1} {{" +
            $"\n{_indentPlaceholder}" + "    get {{" + $"\n{_indentPlaceholder}" +
            $"\n{_indentPlaceholder}" + "        return ResourceManager.GetString(\"{2}\", resourceCulture);" + 
            $"\n{_indentPlaceholder}" + "    }}" + 
            $"\n{_indentPlaceholder}" + "}}";
        private const string _designerExtension = ".Designer.cs";
        private const string _resExtension = ".resx";

        private string _designerFullPath;
        private readonly ILog _logger;
        private readonly List<ResourceFile> _resourceFiles = new List<ResourceFile>();

        public Project(string fullPathToProjectFile, string projectName)
        {
            _logger = LogManager.GetLogger(nameof(Project));
            IsValidResourcesProject = CheckIfIsCorrectResourcesProject(Path.GetDirectoryName(fullPathToProjectFile));
            ProjectName = projectName;
            FullPathToProjectFile = fullPathToProjectFile;
        }

        public string ProjectName { get; }

        public string FullPathToProjectFile { get; }

        /// <inheritdoc/>
        public bool IsValidResourcesProject { get; }

        /// <inheritdoc/>
        public List<CultureInfo> AvailableLanguages { get; private set; }

        public bool CheckIfTranslationKetExists(CultureInfo language, string translationKey)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            return file.CheckIfTranslationKetExists(translationKey);
        }

        public Translation GetTranslation(CultureInfo language, string translationKey)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            return file.GetTranslation(translationKey);
        }

        public Translation[] GetTranslations(CultureInfo language)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            var translations = file.GetTranslations(out string[] duplicatedKeys);
            if (duplicatedKeys.Length > 0)
                DuplicatedKeysFound?.Invoke(duplicatedKeys);
            return translations;
        }

        public bool SaveTranslation(CultureInfo language, Translation newTranslation)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            return file.SaveTranslation(newTranslation)
                && WriteNewPropertyToDesignerFile(newTranslation);
        }

        public bool UpdateTranslation(CultureInfo language, Translation editedTranslation, string originalTranslationKey)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            return file.UpdateTranslation(editedTranslation, originalTranslationKey);
        }

        private bool CheckIfIsCorrectResourcesProject(string directory)
        {
            if (!Directory.Exists(directory)) return false;
            if (!CheckIfDirectoryContainsRequiredFiles(directory))
            {
                var enumerator = Directory.GetDirectories(directory).GetEnumerator();
                bool isCorrect = false;
                // It will stop if directory is found (so first directory satisfying
                // conditions will be loaded (_resourcesFiles will be populated)).
                while (!isCorrect && enumerator.MoveNext())
                    isCorrect = CheckIfIsCorrectResourcesProject((string)enumerator.Current);

                return isCorrect;
            }
            return true;
        }

        /// <summary>
        /// Checks if directory has required files and named correctly, i.e.:
        /// - sample.resx
        /// - sample.*.resx
        /// - sample.Designer.cs
        /// Where 'sample' is just any name and '*' is any string (should be correct abbreviation
        /// of culture).
        /// NOTE: method requires single designer file, to avoid ambiguity.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfDirectoryContainsRequiredFiles(string directory)
        {
            _resourceFiles.Clear();

            var files = Directory.GetFiles(directory);
            var designerFiles = files.Where(f => f.EndsWith(_designerExtension)).ToArray();
            // We do not have any designer file.
            if (designerFiles.Length == 0)
                return false;
            // We have more than one - we do not know what to choose.
            if (designerFiles.Length > 1)
                return false;
            var designerFile = designerFiles.Single();
            _designerFullPath = designerFile;
            // Here we remove designer extension and get only file name.
            var baseFileName = Path.GetFileName(designerFile.Substring(0, designerFile.Length - _designerExtension.Length));
            foreach (var path in files)
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith(baseFileName) && fileName.EndsWith(_resExtension))
                {
                    var isMainResource = 0 == string.Compare(fileName, baseFileName + _resExtension, true);
                    // We want main resource as first element.
                    if (isMainResource)
                        _resourceFiles.Insert(0, new ResourceFile(path, isMainResource));
                    else
                        _resourceFiles.Add(new ResourceFile(path, isMainResource));
                }
            }

            if (_resourceFiles.Count() <= 0) return false;

            AvailableLanguages = _resourceFiles.Select(f => f.CultureInfo).ToList();
            return true;
        }
#warning Perfect for unit tests
        private bool WriteNewPropertyToDesignerFile(Translation translation)
        {
            _logger.Info($"Writing new translation to designer file. Translation {translation}, file {_designerFullPath}");
            var tempFileName = _designerFullPath + "temp";
            StreamReader reader = null;
            StreamWriter writer = null;
            try 
            {
                reader = new StreamReader(_designerFullPath);
                writer = new StreamWriter(tempFileName);
             
                var classDeclarationRead = false;
                var openedBraces = 0;

                var line = string.Empty;
                var indentLineBefore = string.Empty;
                while (!reader.EndOfStream)
                {
                    // Here we remember indentation in previous line (if it was read).
                    // So, when we come across class end, we know what indentation were.
                    if (!string.IsNullOrEmpty(line))
                        indentLineBefore = Regex.Match(line, @"^\s+").Value;

                    line = reader.ReadLine();
                    var isComment = line.Trim().StartsWith("//");
                    // Line is not a comment and contains class keyword.
                    if (!isComment && Regex.IsMatch(line, @"(?:\sclass|class)\s"))
                        classDeclarationRead = true;
                    if (!isComment && classDeclarationRead && line.Contains('{'))
                        openedBraces++;
                    if (!isComment && classDeclarationRead && line.Contains('}'))
                        openedBraces--;

                    if(classDeclarationRead && openedBraces == 0)
                    {
                        // It is important to insert indentations before formatting the string, so we do 
                        // not accidentally insert indent if someone uses our indent placeholder in a translation.
                        var indented = _translationDesignerPropertyFormat.Replace(_indentPlaceholder, indentLineBefore);
                        writer.Write(indented, translation.Text, translation.Key, translation.Key);
                        writer.WriteLine();
                    }

                    writer.WriteLine(line);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Error during saving translation to designer file.", ex);
                return false;
            }
            finally
            {
                reader?.Dispose();
                writer?.Dispose();
            }
            return true;
        }
    }
}
