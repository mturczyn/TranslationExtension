using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using AddTranslationCore.ResourceProjectHelpers;
using System;
using System.Globalization;

namespace AddTranslationTestApplication
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

        public CultureInfo[] AvailableLanguages => _projectHelper.AvailableLanguages;

        public Translation[] GetTranslations()
        {
            if (!_projectHelper.IsValidResourcesDirectory) return Array.Empty<Translation>();
            return _projectHelper.GetTranslations();
        }
    }
}
