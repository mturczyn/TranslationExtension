using AddTranslationUI.Abstractions;
using log4net;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AddTranslationUI
{
    public class AddTranslationViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation and helpers
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Returns whether property changed event was raised.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private bool SetPropertyAndRaise<T>(T value, ref T field, string propertyName)
        {
            if (field.Equals(value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        #endregion

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
            set => SetPropertyAndRaise(value, ref _selectedProject, nameof(SelectedProject));
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
