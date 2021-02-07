using AddTranslationCore.Model;
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
        List<ResourceFile> AvailableLanguages { get; }
        /// <summary>
        /// Gets translations for main language.
        /// </summary>
        /// <returns></returns>
        Translation[] GetTranslations();

        /// <summary>
        /// Saves new translation to current project's dictionary.
        /// </summary>
        /// <param name="newTranslation"></param>
        /// <returns></returns>
        bool SaveTranslation(Translation newTranslation);

        /// <summary>
        /// Updated edited translation based on passed original key.
        /// </summary>
        /// <param name="editedTranslation">Edited translation, that will be saved.</param>
        /// <param name="originalTranslationKey">Key of translation before edition (so it can be found in a file and updated).</param>
        /// <returns></returns>
        bool SaveTranslation(Translation editedTranslation, string originalTranslationKey);
    }
}
