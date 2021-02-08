using AddTranslationCore.Model;
using System.Collections.Generic;

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
        /// <summary>
        /// Contains list of available languages (different from main language).
        /// </summary>
        List<ResourceFile> AvailableLanguages { get; }
    }
}
