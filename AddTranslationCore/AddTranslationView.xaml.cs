namespace AddTranslationCore
{
    /// <summary>
    /// Interaction logic for AddTranslationView.xaml
    /// </summary>
    public partial class AddTranslationView
    {
        public AddTranslationView()
        {
            // Call to package to load it. Otherwise it can be not loaded.
            Microsoft.Xaml.Behaviors.Behavior beh = null;
            InitializeComponent();
        }
    }
}
