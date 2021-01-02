using System.Globalization;

namespace AddTranslationCore.Abstractions
{
    public interface IProjectItem
    {
        string ProjectName { get; }
        string FullPathToProjectFile { get; }
        /// <summary>
        /// Checks if project directory contains correctly placed resources files as well as designer file.
        /// </summary>
        /// <returns></returns>
        bool IsValidResourcesProject { get; }
        string[] GetTranslations(CultureInfo cultureInfo);
    }

}
