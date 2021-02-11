using AddTranslationCore.ViewModel;
using System;
using System.Collections.Generic;
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
        List<CultureInfo> AvailableLanguages { get; }

        Translation[] GetTranslations(CultureInfo language);

        bool SaveTranslation(CultureInfo language, Translation newTranslation);

        bool UpdateTranslation(CultureInfo language, Translation editedTranslation, string originalTranslationKey);

        bool CheckIfTranslationKetExists(CultureInfo language, string translationKey);
    }
}
