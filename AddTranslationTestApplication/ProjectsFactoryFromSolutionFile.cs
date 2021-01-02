using AddTranslationCore.Abstractions;
using log4net;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace AddTranslationTestApplication
{
    class ProjectsFactoryFromSolutionFile : IProjectItemFactory
    {
        /// <summary>
        /// Regular expression to find project references from solution file.
        /// It captures two groups:
        /// - first stored project name in solution,
        /// - second stores it's directory,
        /// </summary>
        private const string REGEX_PROJECTS_FROM_SOLUTION_FILE = @"Project\(""\{[^}]*\}""\)\s*=\s*""([^""]+)"",\s*""([^""]+)";
        private readonly ILog _logger;

        public ProjectsFactoryFromSolutionFile()
        {
            _logger = LogManager.GetLogger(nameof(ProjectsFactoryFromSolutionFile));
        }

        public IProjectItem[] GetProjectItems()
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Choose solution file to provide context";
            ofd.Filter = "Solution files|*.sln";
            var result = ofd.ShowDialog();

            if (!(result ?? false))
            {
                _logger.Info("User cancelled");
                // No projects returned.
                return null;
            }
            var solutionPath = ofd.FileName;
            if (string.IsNullOrEmpty(solutionPath) && File.Exists(solutionPath))
            {
                _logger.Warn($"Something wrong with the chosen file: {solutionPath ?? "NULL"}");
                MessageBox.Show($"Something wrong with the chosen file: {solutionPath ?? "NULL"}. Check if file exists.");
                return null;
            }

            var regexMatches = Regex.Matches(
                File.ReadAllText(solutionPath),
                REGEX_PROJECTS_FROM_SOLUTION_FILE);

            if (regexMatches.Count == 0)
            {
                var msg = "Solution does not contain any project references.";
                MessageBox.Show(msg);
                _logger.Warn(msg);
            }

            var projectItems = new List<IProjectItem>();
            var slnDir = Path.GetDirectoryName(solutionPath);
            for (int i = 0; i < regexMatches.Count; i++)
            {
                var match = regexMatches[i];
                if (match.Groups.Count < 3)
                {
                    var errMsg = $"Could not fully parse project reference for match {match.Value}.";
                    _logger.Error(errMsg);
                    MessageBox.Show(errMsg);
                    continue;
                }
                var projName = match.Groups[1].Value;
                var projRelativePath = match.Groups[2].Value;

                var pi = new ProjectItem(Path.GetFullPath(Path.Combine(slnDir, projRelativePath)), projName);
                if (pi.IsValidResourcesProject)
                    projectItems.Add(pi);
            }

            return projectItems.ToArray();
        }
    }
}
