using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AddTranslationUI
{
    public class AddTranslationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IProjectIem> ProjectReferences { get; } = new ObservableCollection<IProjectIem>();

        public AddTranslationViewModel()
        {
            var logger = log4net.LogManager.GetLogger("Logger");
            logger.Info("Logging something");
        }
    }
}
