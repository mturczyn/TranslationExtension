using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using AddTranslationCore.ViewModel;
using log4net;
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

        public AddTranslationViewModel(IProjectItemFactory projectItemFactory)
        {
            _logger = LogManager.GetLogger(nameof(AddTranslationViewModel));

            if (projectItemFactory == null)
            {
                _logger.Error($"{nameof(projectItemFactory)} is null.");
                throw new System.ArgumentNullException(nameof(projectItemFactory));
            }

            _projectItemFactory = projectItemFactory;
            
            Translations.Source = _translations;

            LoadProjects();
        }

        public ICommand TestCommand { get; } = new RelayCommand((param) =>
        {
            int i = 0;
        });

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

        private string _originalText;
        public string OriginalText
        {
            get => _originalText;
            set
            {
                if (!Set(value, ref _originalText)) return;
                if(!string.IsNullOrEmpty(OriginalText))
                    SortTranslations();
            }
        }

        private string _translationText;
        public string TranslationText
        {
            get => _translationText;
            set => Set(value, ref _translationText);
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
            var comparer = new TranslationComparer(OriginalText);
            _translations.Sort(comparer);
            Translations.View.Refresh();
        }
    }
}
