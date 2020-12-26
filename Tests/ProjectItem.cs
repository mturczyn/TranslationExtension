using AddTranslationUI.Abstractions;
using AddTranslationUI.ResourceProjectHelpers;
using System.Globalization;

namespace Tests
{
    class ProjectItem : IProjectItem
    {
        private readonly ResourcesProjectHelper _projectHelper;

        public ProjectItem(ResourcesProjectHelper projectHelper, string projectDirectory, string projectName) 
            => (_projectHelper, FullPathToProjectFile, ProjectName) = (projectHelper, projectDirectory, projectName);
        public ProjectItem(string projectDirectory, string projectName) 
            => (_projectHelper, FullPathToProjectFile, ProjectName) = (new ResourcesProjectHelper(projectDirectory), projectDirectory, projectName);

        public string ProjectName { get; set; }
        public string FullPathToProjectFile { get; set; }
        public bool IsValidResourcesProject => _projectHelper.IsValidResourcesDirectory;

        public string[] GetTranslations(CultureInfo culture)
        {
            return _projectHelper.GetTranslations(culture);
        }
    }
}
