using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace AddTranslationUI
{
    /// <summary>
    /// Interaction logic for AddTranslationView.xaml
    /// </summary>
    public partial class AddTranslationView : UserControl
    {
        public AddTranslationView()
        {
            InitializeComponent();
        }

        public ObservableCollection<IProjectIem> ProjectReferences { get; } = new ObservableCollection<IProjectIem>();
    }
}
