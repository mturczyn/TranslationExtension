using AddTranslationCore.Abstractions;
using AddTranslationCore.DTO;
using AddTranslationCore.ViewModel;
using log4net;
using System.Collections.ObjectModel;

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
        public RelayCommand TestCommand { get; } = new RelayCommand((param) =>
        {
            int i = 0;
        });
        public ObservableCollection<IProjectItem> ProjectReferences { get; } = new ObservableCollection<IProjectItem>();

        public ObservableCollection<Translation> Translations { get; } = new ObservableCollection<Translation>();

        private IProjectItem _selectedProject;
        public IProjectItem SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (!SetPropertyAndRaise(value, ref _selectedProject, nameof(SelectedProject))) return;
                SetTranslations();
            }
        }

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set => SetPropertyAndRaise(value, ref _translationKey, nameof(TranslationKey));
        }

        private string _originalText;
        public string OriginalText
        {
            get => _originalText;
            set => SetPropertyAndRaise(value, ref _originalText, nameof(OriginalText));
        }

        private string _translationText;
        public string TranslationText
        {
            get => _translationText;
            set => SetPropertyAndRaise(value, ref _translationText, nameof(TranslationText));
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
    }
}
