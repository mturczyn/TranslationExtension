using AddTranslationCore.Abstractions;
using AddTranslationCore.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AddTranslationCore.ViewModel
{
    public class Project : BaseObservable, IProjectItem
    {
        public event Action<string[]> DuplicatedKeysFound;

        private static readonly string _designerExtension = ".Designer.cs";
        private static readonly string _resExtension = ".resx";

        private readonly ILog _logger;

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
        public List<ResourceFile> AvailableLanguages { get; } = new List<ResourceFile>();

        public Translation GetTranslation(CultureInfo cultureInfo, string translationKey)
        {
            var file = AvailableLanguages.Single(f => f.CultureInfo.LCID == cultureInfo.LCID);
            return file.GetTranslation(translationKey);
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
            AvailableLanguages.Clear();

            var files = Directory.GetFiles(directory);
            var designerFiles = files.Where(f => f.EndsWith(_designerExtension)).ToArray();
            // We do not have any designer file.
            if (designerFiles.Length == 0)
                return false;
            // We have more than one - we do not know what to choose.
            if (designerFiles.Length > 1)
                return false;
            var designerFile = designerFiles.Single();
            // Here we remove designer extension and get only file name.
            var baseFileName = Path.GetFileName(designerFile.Substring(0, designerFile.Length - _designerExtension.Length));
            foreach (var path in files)
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith(baseFileName) && fileName.EndsWith(_resExtension))
                {
                    var isMainResource = 0 == string.Compare(fileName, baseFileName + _resExtension, true);
                    // We want main resource as first element.
                    if(isMainResource)
                        AvailableLanguages.Insert(0, new ResourceFile(path, isMainResource));
                    else
                        AvailableLanguages.Add(new ResourceFile(path, isMainResource));
                }
            }

            if (AvailableLanguages.Count() <= 0) return false;
            return true;
        }

        /// <inheritdoc/>
        public bool SaveTranslation(Translation newTranslation)
        {
            var resFile = AvailableLanguages.Single(f => f.IsMainResource);
            return false;
        }

        /// <inheritdoc/>
        public bool SaveTranslation(Translation editedTranslation, string originalTranslationKey)
        {
            return false;
        }
    }
}
