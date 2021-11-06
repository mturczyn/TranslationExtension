using AddTranslation.Core.Abstractions;
using AddTranslation.Core.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AddTranslation.Core.ViewModel
{
    public class Project : BaseObservable, IProjectItem
    {
        public event Action<string[]> DuplicatedKeysFound;
        // Output folders.
        private const string _binDirectoryName = "bin";
        private const string _objDirectoryName = "obj";
        /// <summary>
        /// This is placeholder used to hold place and allow easy insertion
        /// of necessary indent in a property.
        /// </summary>
        private const string _indentPlaceholder = "_I_N_D_E_N_T_";
        private const string _designerPropertyPattern = @"\s*(?:public|internal) static string {0}";
        private const string _returnPattern = @"\s*return ResourceManager\.GetString\(""{0}"", resourceCulture\);";
        /// <summary>
        /// Formatable string holding pattern for field generated in designer file.
        /// </summary>
        private static string _translationDesignerPropertyFormat =
              $"{_indentPlaceholder}" + "/// <summary>" +
            $"\n{_indentPlaceholder}" + "///     Looks up a localized string similar to {0}." +
            $"\n{_indentPlaceholder}" + "/// </summary>" +
            $"\n{_indentPlaceholder}" + "public static string {1} {{" +
            $"\n{_indentPlaceholder}" + "    get {{" +
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

        public bool UpdateTranslation(CultureInfo language, Translation newTranslation, Translation originalTranslation)
        {
            var file = _resourceFiles.Single(f => f.CultureInfo.LCID == language.LCID);
            return file.UpdateTranslation(newTranslation, originalTranslation.Key)
                && UpdatePropertyInDesignerFile(originalTranslation, newTranslation);
        }

        private bool CheckIfIsCorrectResourcesProject(string directory)
        {
            if (!Directory.Exists(directory)) return false;

            if (CheckIfDirectoryContainsRequiredFiles(directory))
                return true;
            else
            {
                var subdirectories = Directory.GetDirectories(directory);
                // Recursion will stop if directory is found (so first directory satisfying
                // conditions will be loaded (_resourcesFiles will be populated)).
                foreach (var subdir in subdirectories)
                {
                    // Do not recurse into bin and obj directories.
                    if (subdir == Path.Combine(directory, _binDirectoryName)
                        || subdir == Path.Combine(directory, _objDirectoryName))
                        continue;
                    if (CheckIfIsCorrectResourcesProject(subdir))
                        return true;
                }
                return false;
            }
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
#warning Good candidate for consumer-writer pattern and use of Channels
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
                    // Write new property right before last brace.
                    if (classDeclarationRead && openedBraces == 0)
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
            catch (Exception ex)
            {
                _logger.Error("Error during saving translation to designer file.", ex);
                return false;
            }
            finally
            {
                reader?.Dispose();
                writer?.Dispose();
            }

            return DeleteDesignerFileAndRenameTempFile(_designerFullPath, tempFileName);
        }

        private bool DeleteDesignerFileAndRenameTempFile(string designerPath, string tempFilePath)
        {
            try
            {
                File.Delete(designerPath);
                File.Move(tempFilePath, designerPath);
            }
            catch (Exception ex)
            {
                _logger.Error("Error during cleaning designer files.", ex);
                return false;
            }
            return true;
        }

        private bool UpdatePropertyInDesignerFile(Translation oldTranslation, Translation newTranslation)
        {
            _logger.Info($"Updating translation in designer file. Old translation {oldTranslation}, new translation {newTranslation}, file {_designerFullPath}");
            var tempFileName = _designerFullPath + "temp";
            StreamReader reader = null;
            StreamWriter writer = null;
#warning May be replaced by channel, when using asynchronous methods.
            var queueCapacity = 5;
            var queue = new Queue<string>(queueCapacity);
            try
            {
                reader = new StreamReader(_designerFullPath);
                writer = new StreamWriter(tempFileName);

                var propertyFound = false;
                var classDeclarationRead = false;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    // If we found property, then we simply rewrite rest of file.
                    if (propertyFound)
                    {
                        writer.WriteLine(line);
                        continue;
                    }

                    var isComment = line.TrimStart().StartsWith("//");
                    // Line is not a comment and contains class keyword.
                    if (!isComment && Regex.IsMatch(line, @"(?:\sclass|class)\s"))
                        classDeclarationRead = true;
                    // If we are not yet inside the class, then just re-write lines.
                    if (!classDeclarationRead)
                    {
                        writer.WriteLine(line);
                        continue;
                    }
                    // If we found what we are looking for, we flush the queue and
                    // do the processing (text replacement).
                    if (Regex.IsMatch(line, string.Format(_designerPropertyPattern, oldTranslation.Key)))
                    {
                        _logger.Info("Found translation, replacing key and text.");
                        // Flush the queue, when found a comment containing text process it appropriately.
                        while (queue.Count > 0)
                        {
                            var lineFromQueue = queue.Dequeue();
                            // Here we try to replace text in comment.
                            if (lineFromQueue.TrimStart().StartsWith("//"))
                                lineFromQueue = lineFromQueue.Replace(oldTranslation.Text, newTranslation.Text);
                            writer.WriteLine(lineFromQueue);
                        }
                        line = line.Replace(oldTranslation.Key, newTranslation.Key);
                        writer.WriteLine(line);

                        var i = 0;
                        // For next four lines we try to rewrite following lines, but we look for
                        // return statement, which contains translation key to replace.
                        while (!reader.EndOfStream && i < 4)
                        {
                            var innerLine = reader.ReadLine();
                            if (Regex.IsMatch(innerLine, string.Format(_returnPattern, oldTranslation.Key)))
                            {
                                _logger.Info("Found return statement.");
                                innerLine = innerLine.Replace(oldTranslation.Key, newTranslation.Key);
                            }
                            writer.WriteLine(innerLine);
                            i++;
                        }

                        propertyFound = true;
                    }
                    else
                    {
                        // If we reach the limit of a queue, we write last item.
                        if (queue.Count == queueCapacity)
                            writer.WriteLine(queue.Dequeue());

                        queue.Enqueue(line);
                    }
                }
                // Flush rest of the queue.
                while (queue.Count > 0) writer.WriteLine(queue.Dequeue());
            }
            catch (Exception ex)
            {
                _logger.Error("Error during updating translation to designer file.", ex);
                return false;
            }
            finally
            {
                reader?.Dispose();
                writer?.Dispose();
            }

            return DeleteDesignerFileAndRenameTempFile(_designerFullPath, tempFileName);
        }
    }
}
