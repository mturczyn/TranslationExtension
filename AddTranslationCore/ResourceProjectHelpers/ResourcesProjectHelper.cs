using log4net;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AddTranslationCore.ResourceProjectHelpers
{

    public class ResourcesProjectHelper
    {
        private ResourceFile[] _resourcesFiles;
        private readonly ILog _logger;

        public ResourcesProjectHelper(string projectDirectory)
        {
            _logger = LogManager.GetLogger(nameof(ResourcesProjectHelper));
            IsValidResourcesDirectory = CheckIfIsCorrectResourcesProject(Path.GetDirectoryName(projectDirectory));
        }

        public bool IsValidResourcesDirectory { get; }

        public string[] GetTranslations(CultureInfo cultureInfo)
        {
            var file = _resourcesFiles.Single(f => f.CultureInfo.LCID == cultureInfo.LCID);
            var translations = file.GetTranslations();
            return new string[] { "Hello world" };
        }
        
        private bool CheckIfIsCorrectResourcesProject(string directory)
        {
            if (! Directory.Exists(directory)) return false;
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
        private bool CheckIfDirectoryContainsRequiredFiles(string directory, out ResourceFile[] resourceFiles)
        {
            var designerExtension = ".Designer.cs";
            var resExtension = ".resx";
            resourceFiles = null;

            var files = Directory.GetFiles(directory);
            var designerFiles = files.Where(f => f.EndsWith(designerExtension)).ToArray();
            // We do not have any designer file.
            if (designerFiles.Length == 0)
                return false;
            // We have more than one - we do not know what to choose.
            if (designerFiles.Length > 1)
                return false;
            var designerFile = designerFiles.Single();
            // Here we remove designer extension and get only file name.
            var baseFileName = Path.GetFileName(designerFile.Substring(0, designerFile.Length - designerExtension.Length));
            // We get all resource files with translations.
            var resFiles = files.Where(f =>
            {
                var fn = Path.GetFileName(f);
                return fn.EndsWith(resExtension) && fn.StartsWith(baseFileName);
            }).Select(f =>
            {
                var fn = Path.GetFileName(f);
                return new ResourceFile(f, fn == baseFileName + resExtension);
            });

            if (resFiles.Count() <= 0) return false;
            // Set found resources files.
            resourceFiles = resFiles.ToArray();
            return true;
        }
    }
}
