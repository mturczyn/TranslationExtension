using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using log4net;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AddTranslationCore.ResourceProjectHelpers
{
    public class Project : IProjectItem
    {
        private static readonly string _designerExtension = ".Designer.cs";
        private static readonly string _resExtension = ".resx";

        private ResourceFile[] _resourcesFiles;
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

        public bool IsValidResourcesProject { get; }

        public CultureInfo[] AvailableLanguages => _resourcesFiles.Where(rf => !rf.IsMainResource).Select(rf => rf.CultureInfo).ToArray();

        public Translation[] GetTranslations()
        {
            var file = _resourcesFiles.Single(f => f.IsMainResource);
            var translations = file.GetTranslations(out string[] duplicatedKeys).ToArray();

        }

        public Translation GetTranslation(CultureInfo cultureInfo, string translationKey)
        {
            var file = _resourcesFiles.Single(f => f.CultureInfo.LCID == cultureInfo.LCID);
            return file.GetTranslation(translationKey);
        }

        private bool CheckIfIsCorrectResourcesProject(string directory)
        {
            if (!Directory.Exists(directory)) return false;
            if (!CheckIfDirectoryContainsRequiredFiles(directory, out _resourcesFiles))
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
        private static bool CheckIfDirectoryContainsRequiredFiles(string directory, out ResourceFile[] resourceFiles)
        {
            resourceFiles = null;

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
            // We get all resource files with translations.
            List<ResourceFile> resFiles = new List<ResourceFile>();
            foreach (var path in files)
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith(baseFileName) && fileName.EndsWith(_resExtension))
                    resFiles.Add(new ResourceFile(path, 0 == string.Compare(fileName, baseFileName + _resExtension, true)));
            }

            if (resFiles.Count() <= 0) return false;
            // Set found resources files.
            resourceFiles = resFiles.ToArray();
            return true;
        }
    }
}
