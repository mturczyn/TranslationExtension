using log4net;
using System.IO;
using System.Linq;

namespace AddTranslationUI.ResourceProjectHelpers
{

    public class ResourcesProjectHelper
    {
        private ResourceFile[] _resourcesFiles;
        private readonly ILog _logger;

        public bool DirectorySet { get; }

        public ResourcesProjectHelper(string projectDirectory)
        {
            DirectorySet = CheckIfIsCorrectResourcesProject(projectDirectory);
            _logger = LogManager.GetLogger(nameof(ResourcesProjectHelper));
        }

        private bool CheckIfIsCorrectResourcesProject(string directory)
        {
            if (! Directory.Exists(directory)) return false;
            if (!CheckIfDirectoryContainsRequiredFiles(directory))
            {
                var isCorrect = false;
                foreach (var subdirectory in Directory.GetDirectories(directory))
                {
                    isCorrect = isCorrect || CheckIfIsCorrectResourcesProject(subdirectory);
                }
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
            var designerExtension = ".Designer.cs";
            var resExtension = ".resx";

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
            var baseFileName = Path.GetFileName(designerFile.Substring(designerFile.Length - designerExtension.Length));
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
            _resourcesFiles = resFiles.ToArray();
            return true;
        }
    }
}
