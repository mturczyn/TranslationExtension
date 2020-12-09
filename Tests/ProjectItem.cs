using AddTranslationUI.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tests
{
    class ProjectItem : IProjectIem
    {
        public string ProjectName { get; set; }
        public string FullPathToProjectFile { get; set; }

        public bool CheckIfIsCorrectResourcesProject()
        {

        }

        /// <summary>
        /// Checks if directory has required files and named correctly, i.e.:
        /// - sample.resx
        /// - sample.*.resx
        /// - sample.Designer.cs
        /// Where 'sample' is just any name and '*' is any string (should be correct abbreviation
        /// of culture).
        /// </summary>
        /// <returns></returns>
        private bool CheckIfDirectoryContainsRequiredFiles(string directory)
        {
            var files = Directory.GetFiles(directory);
            var designerFiles = files.Where(f => f.EndsWith(".Designer.cs")).ToArray();
            // We do not have any designer file.
            if(designerFiles.Length == 0)
                return false;
            // We have more than one - we do not know what to choose.
            if (designerFiles.Length > 1)
                return false;

            // Inform user what went wrong - he has too many files?
            // Check rouserces files.
        }
    }
}
