﻿using AddTranslation.Core.Abstractions;
using AddTranslation.Core.ViewModel;
using log4net;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace AddTranslation.Core
{
    public class AddTranslationViewModel : BaseObservable
    {
        private readonly List<Translation> _translations = new List<Translation>();
        private readonly IProjectItemFactory _projectItemFactory;
        private readonly ILog _logger;
        private Translation _editedTranslation;
        private readonly Dispatcher _visualStudioDispatcher;

        /// <summary>
        /// It is little bit somehow against the MVVM pattern. But we need to close
        /// the window in some cases, so for conveniency I store the reference.
        /// Should not be used anywhere else.
        /// </summary>
        private readonly Window _view;

        public AddTranslationViewModel(IProjectItemFactory projectItemFactory, Window view)
        {
            _visualStudioDispatcher = Application.Current.MainWindow.Dispatcher;
            _view = view;

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            _logger = LogManager.GetLogger(nameof(AddTranslationViewModel));
            if (projectItemFactory == null)
            {
                _logger.Error($"{nameof(projectItemFactory)} is null.");
                throw new ArgumentNullException(nameof(projectItemFactory));
            }

            _projectItemFactory = projectItemFactory;
            Translations.Source = _translations;
            _ = LoadProjects();
        }

        private ICommand _editTranslationCommand;
        public ICommand EditTranslationCommand => _editTranslationCommand ?? (_editTranslationCommand = new RelayCommand<Translation>(EditTranslation));

        private ICommand _saveTranslationEditCommand;
        public ICommand SaveTranslationEditCommand => _saveTranslationEditCommand ?? (_saveTranslationEditCommand = new RelayCommand<Translation>(SaveTranslationEdit));

        private ICommand _cancelTranslationEditCommand;
        public ICommand CancelTranslationEditCommand => _cancelTranslationEditCommand ?? (_cancelTranslationEditCommand = new RelayCommand<Translation>(CancelTranslationEdit));

        private ICommand _saveNewTranslationCommand;
        public ICommand SaveNewTranslationCommand => _saveNewTranslationCommand ?? (_saveNewTranslationCommand = new RelayCommand(SaveNewTranslation));

        private ICommand _viewLoadedCommand;
        public ICommand ViewLoadedCommand => _viewLoadedCommand ?? (_viewLoadedCommand = new RelayCommand(ViewLoaded));

        public ObservableCollection<IProjectItem> ProjectReferences { get; } = new ObservableCollection<IProjectItem>();

        public CollectionViewSource Translations { get; } = new CollectionViewSource();

        public ObservableCollection<CultureInfo> AvailableLanguages { get; } = new ObservableCollection<CultureInfo>();

        private CultureInfo _selectedLanguage;
        public CultureInfo SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (!Set(value, ref _selectedLanguage)) return;
                RaisePropertyChanged(nameof(IsLanguageSelected));
            }
        }

        public bool IsLanguageSelected => SelectedLanguage != null;

        public bool IsKeyCorrect => string.IsNullOrEmpty(ErrorText);

        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set => Set(value, ref _errorText);
        }

        private IProjectItem _selectedProject;
        public IProjectItem SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (_selectedLanguage != null)
                    _selectedProject.DuplicatedKeysFound -= DuplicatedTranslationKeysFound;
                if (value != null)
                    value.DuplicatedKeysFound += DuplicatedTranslationKeysFound;
                if (!Set(value, ref _selectedProject)) return;
                SetAvailableLanguages();
                SetTranslations();
            }
        }

        private Translation _selectedTranslation;
        public Translation SelectedTranslation
        {
            get => _selectedTranslation;
            set
            {
                var prevSelected = _selectedTranslation;
                if (!Set(value, ref _selectedTranslation)) return;
                // If we switch from translation that was edited, we cancel that edition.
                if (prevSelected?.IsUnderEdition ?? false)
                {
                    prevSelected.Text = _editedTranslation.Text;
                    prevSelected.Key = _editedTranslation.Key;
                    prevSelected.IsUnderEdition = false;
                }
            }
        }

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set
            {
                if (!Set(value, ref _translationKey)) return;
                ErrorText = ValidateTranslationKey(TranslationKey);
                RaisePropertyChanged(nameof(IsKeyCorrect));
            }
        }

        private string _translationText;
        public string TranslationText
        {
            get => _translationText;
            set
            {
                if (!Set(value, ref _translationText)) return;
                SortTranslations();
            }
        }

        private async Task LoadProjects()
        {
            ProjectReferences.Clear();
            var projectItems = await _projectItemFactory.GetProjectItems().ConfigureAwait(false);
            if (projectItems.Length == 0)
            {
                _logger.Warn("Loading project returned 0 projects");
                MessageBox.Show("Did not find any projects.");
                return;
            }
            var validProjects = projectItems.Where(p => p.IsValidResourcesProject);
            if (!validProjects.Any())
            {
                var warning = "Did not find any projects that Have valid language resources.";
                _logger.Warn(warning);
                MessageBox.Show(warning);
                return;
            }
            foreach (var p in validProjects)
            {
                ProjectReferences.Add(p);
            }
        }

        private void SetTranslations()
        {
            if (SelectedLanguage == null)
            {
                _logger.Error("Trying to set translations, but language was not selected");
                return;
            }
            if (SelectedProject == null)
            {
                _logger.Error("Trying to set translations, but project was not selected");
                return;
            }
            _translations.Clear();
            foreach (var t in SelectedProject.GetTranslations(SelectedLanguage)) _translations.Add(t);
            Translations.View.Refresh();
        }

        private void SetAvailableLanguages()
        {
            AvailableLanguages.Clear();
            foreach (var t in SelectedProject.AvailableLanguages)
                AvailableLanguages.Add(t);

            SelectedLanguage = AvailableLanguages.First();
        }

        private void SortTranslations()
        {
            var comparer = new TranslationComparer(TranslationText);
            _translations.Sort(comparer);
            Translations.View.Refresh();
        }

        private void SaveNewTranslation()
        {
            if (SelectedLanguage == null)
            {
                _logger.Warn("Trying to save new translation, but language was not selected");
                MessageBox.Show("Please select language before working with translations.");
                return;
            }
            if (CheckIfTranslationKeyExists(TranslationKey))
            {
                MessageBox.Show($"Translation key \"{TranslationKey}\" already in use. Keys must be unique.");
                return;
            }
            var translation = new Translation(TranslationKey, TranslationText, SelectedLanguage);
            if (!SelectedProject.SaveTranslation(SelectedLanguage, translation))
            {
                MessageBox.Show("Saving of translation failed. Please ensure correctness of resource file.");
            }
            else
            {
                TranslationKey = string.Empty;
                TranslationText = string.Empty;
                // Refresh after successful save.
                SetTranslations();
            }
        }

        private void ViewLoaded()
        {
            RaisePropertyChanged(nameof(IsKeyCorrect));
            RaisePropertyChanged(nameof(IsLanguageSelected));
        }

        private void SaveTranslationEdit(Translation translation)
        {
            if (SelectedLanguage == null)
            {
                _logger.Warn("Trying to save edited translation, but language was not selected");
                MessageBox.Show("Please select language before working with translations.");
                return;
            }
            if (_editedTranslation == null)
            {
                _logger.Error($"Trying to edit translation, but {nameof(_editedTranslation)} is null");
                MessageBox.Show("Something went really wrong. Please report issue on GitHub repo https://github.com/mturczyn/TranslationExtension/issues");
                CloseTranslationWindow();
                return;
            }
            var error = ValidateTranslationKey(translation.Key);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.Debug($"Incorrect key in edited translation {translation}");
                MessageBox.Show($"Invalid key, error: {error}");
                return;
            }
            if (!SelectedProject.UpdateTranslation(SelectedLanguage, translation, _editedTranslation))
            {
                MessageBox.Show("Failed updating translation.");
                translation.Text = _editedTranslation.Text;
                translation.Key = _editedTranslation.Key;
            }
            translation.IsUnderEdition = false;
            _editedTranslation = null;
        }

        private void EditTranslation(Translation translation)
        {
            _logger.Info($"User started edition of {translation.Key}");
            translation.IsUnderEdition = true;
            _editedTranslation = (Translation)translation.Clone();
        }

        private void CancelTranslationEdit(Translation translation)
        {
            _logger.Info($"User canceled edition of {_editedTranslation.Key}");
            translation.IsUnderEdition = false;
            translation.Key = _editedTranslation.Key;
            translation.Text = _editedTranslation.Text;
            _editedTranslation = null;
        }

        private void DuplicatedTranslationKeysFound(string[] duplicatedKeys)
            => MessageBox.Show($"Found duplicated keys of translations. " +
                $"You should resolve those duplicates before making any editions and adding new translations:" +
                $"\n{string.Join(", ", duplicatedKeys)}", "Duplicated keys found", MessageBoxButton.OK, MessageBoxImage.Warning);

#warning TODO: maybe should be in another class
        private static string ValidateTranslationKey(string translationKey)
        {
            if (string.IsNullOrWhiteSpace(translationKey))
                return "Translation key must not be empty.";
            else if (Regex.IsMatch(translationKey, @"\s+"))
                return "Translation key must not contain white spaces.";
            // We leave the most expensive check last, after most obvious, as fallback.
            else if (!CodeDomProvider.CreateProvider("C#").IsValidIdentifier(translationKey))
                return "Translation key must be valid C# variable name.";
            else
                return string.Empty;
        }

        private bool CheckIfTranslationKeyExists(string translationKey) => SelectedProject.CheckIfTranslationKetExists(SelectedLanguage, translationKey);

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var unobservedEx in e.Exception.InnerExceptions)
                _logger.Error("Unobserved exception caught.", unobservedEx);
            MessageBox.Show($"There was unobserved exception, showing first of aggregated exceptions:\n{e.Exception.InnerExceptions.First()}\n" +
                $"Please report issue at GitHub repo https://github.com/mturczyn/TranslationExtension/issues",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            CloseTranslationWindow();
        }

        private void CloseTranslationWindow()
        {
            _logger.Info("Closing application.");
            InvokeOnUIThread(() =>
            {
                _view.Close();
            });
        }

        private void InvokeOnUIThread(Action action) => _visualStudioDispatcher.Invoke(action);
    }
}
