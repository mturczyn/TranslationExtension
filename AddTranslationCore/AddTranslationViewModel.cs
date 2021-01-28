using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using AddTranslationCore.ViewModel;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace AddTranslationCore
{
    public class AddTranslationViewModel : BaseObservable
    {
        private readonly List<Translation> _translations = new List<Translation>();
        private readonly IProjectItemFactory _projectItemFactory;
        private readonly ILog _logger;
        private Translation _editedTranslation;

        public AddTranslationViewModel(IProjectItemFactory projectItemFactory)
        {
            _logger = LogManager.GetLogger(nameof(AddTranslationViewModel));

            if (projectItemFactory == null)
            {
                _logger.Error($"{nameof(projectItemFactory)} is null.");
                throw new ArgumentNullException(nameof(projectItemFactory));
            }

            _projectItemFactory = projectItemFactory;
            Translations.Source = _translations;
            LoadProjects();
        }

        private ICommand _editTranslationCommand;
        public ICommand EditTranslationCommand => _editTranslationCommand ?? (_editTranslationCommand = new RelayCommand<Translation>(EditTranslation));

        private ICommand _saveTranslationEditCommand;
        public ICommand SaveTranslationEditCommand => _saveTranslationEditCommand ?? (_saveTranslationEditCommand = new RelayCommand<Translation>(SaveTranslationEdit));

        private ICommand _cancelTranslationEditCommand;
        public ICommand CancelTranslationEditCommand => _cancelTranslationEditCommand ?? (_cancelTranslationEditCommand = new RelayCommand<Translation>(CancelTranslationEdit));

        private ICommand _saveNewTranslationCommand;
        public ICommand SaveNewTranslationCommand => _saveNewTranslationCommand ?? (_saveNewTranslationCommand = new RelayCommand<Translation>(SaveNewTranslation));

        public ObservableCollection<IProjectItem> ProjectReferences { get; } = new ObservableCollection<IProjectItem>();

        public CollectionViewSource Translations { get; } = new CollectionViewSource();

        public ObservableCollection<CultureInfo> AvailableLanguages { get; } = new ObservableCollection<CultureInfo>();

        private IProjectItem _selectedProject;
        public IProjectItem SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (!Set(value, ref _selectedProject)) return;
                SetTranslations();
                SetAvailableLanguages();
            }
        }

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set => Set(value, ref _translationKey);
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

        private void LoadProjects()
        {
            ProjectReferences.Clear();
            var projectItems = _projectItemFactory.GetProjectItems();
            foreach (var p in projectItems) ProjectReferences.Add(p);
        }

        private void SetTranslations()
        {
            _translations.Clear();
            foreach (var t in SelectedProject.GetTranslations()) _translations.Add(t);
            Translations.View.Refresh();
        }

        private void SetAvailableLanguages()
        {
            AvailableLanguages.Clear();
            foreach (var t in SelectedProject.AvailableLanguages) AvailableLanguages.Add(t);
        }

        private void SortTranslations()
        {
            var comparer = new TranslationComparer(TranslationText);
            _translations.Sort(comparer);
            Translations.View.Refresh();
        }

        private void SaveNewTranslation(Translation translation)
        {

        }

        private void SaveTranslationEdit(Translation translation)
        {

        }

        private void EditTranslation(Translation translation)
        {
            _logger.Info($"User started edition of {_editedTranslation.TranslationKey}");
            translation.IsUnderEdition = true;
            _editedTranslation = (Translation)translation.Clone();
        }

        private void CancelTranslationEdit(Translation translation)
        {
            _logger.Info($"User canceled edition of {_editedTranslation.TranslationKey}");
            translation.IsUnderEdition = false;
            _editedTranslation = null;
        }
    }
}
