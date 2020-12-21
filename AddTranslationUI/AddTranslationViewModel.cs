using AddTranslationUI.Abstractions;
using log4net;
using System.Collections.ObjectModel;

namespace AddTranslationUI
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

        public ObservableCollection<IProjectItem> ProjectReferences { get; } = new ObservableCollection<IProjectItem>();

        private IProjectItem _selectedProject;
        public IProjectItem SelectedProject
        {
            get => _selectedProject;
            set
            {
                if(!SetPropertyAndRaise(value, ref _selectedProject, nameof(SelectedProject))) return;

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
    }
}
