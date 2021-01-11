using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using AddTranslationCore.ViewModel;
using log4net;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace AddTranslationCore
{
    public class AddTranslationViewModel : BaseObservable
    {
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
            LoadProjects();
        }
        public ICommand TestCommand { get; } = new RelayCommand((param) =>
        {
            int i = 0;
        });
        public ObservableCollection<IProjectItem> ProjectReferences { get; } = new ObservableCollection<IProjectItem>();

        public ObservableCollection<Translation> Translations { get; } = new ObservableCollection<Translation>();

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
            set => Set(value, ref _originalText);
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
            Translations.Clear();
            foreach (var t in SelectedProject.GetTranslations()) Translations.Add(t);
        }

        private void SetAvailableLanguages()
        {
            AvailableLanguages.Clear();
            foreach (var t in SelectedProject.AvailableLanguages) AvailableLanguages.Add(t);
        }
    }
}
