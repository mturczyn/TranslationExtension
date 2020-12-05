using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddTranslationUINetFramework
{
    public class AddTranslationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IProjectIem> ProjectReferences { get; } = new ObservableCollection<IProjectIem>();

    }
}
