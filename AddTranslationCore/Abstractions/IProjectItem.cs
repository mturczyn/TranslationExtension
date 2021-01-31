using AddTranslationCore.ViewModel;
using System;
using System.Globalization;

namespace AddTranslationCore.Abstractions
{
    public interface IProjectItem
    {
        event Action<string[]> DuplicatedKeysFound;

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
        CultureInfo[] AvailableLanguages { get; }
        /// <summary>
        /// Gets translations for main language.
        /// </summary>
        /// <returns></returns>
        Translation[] GetTranslations();
    }
}
