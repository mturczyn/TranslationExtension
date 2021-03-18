using AddTranslationCore;
using AddTranslationVsix.Windows;
using EnvDTE;
using log4net;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace AddTranslation
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddTranslationCommand
    {
        private readonly ILog _logger;

        private AsyncPackage _serviceProvider;
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("52faa2b6-add8-48ca-89c4-e2487d1d625b");

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTranslationCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AddTranslationCommand(OleMenuCommandService commandService, AsyncPackage package)
        {
            _logger = LogManager.GetLogger(nameof(AddTranslationCommand));

            if (package == null) throw new ArgumentNullException(nameof(package));
            if (commandService == null) throw new ArgumentNullException(nameof(commandService));
            _serviceProvider = package;
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddTranslationCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // For some reason, not all DLLs are loaded in extension (this how VS experimental instance loads them),
            // so here we manually load assembly and then resources.
            try
            {
                System.Reflection.Assembly.Load("AddTranslationCore");

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslationCore;component/UserInterfaceResources/ColorsAndBrushes.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslationCore;component/UserInterfaceResources/SmallStylesForControls.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslationCore;component/UserInterfaceResources/Button.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslationCore;component/UserInterfaceResources/ScrollBar.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslationCore;component/UserInterfaceResources/ComboBox.xaml", UriKind.RelativeOrAbsolute)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load AddTranslationCore assembly.\nException: {ex.Message}\nPlease report this at GitHub repo https://github.com/mturczyn/TranslationExtension/issues", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Switch to the main thread - the call to AddCommand in AddTranslationCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Assumes.Present(commandService);
            // Get Visual Studio extensibility object.
            var dte = (DTE)await package.GetServiceAsync(typeof(DTE));
            Assumes.Present(dte);

            Instance = new AddTranslationCommand(commandService, package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            _logger.Info("Rozpoczęcie tłumaczenia");

            var textManager = (VsTextManager)await _serviceProvider.GetServiceAsync(typeof(SVsTextManager));
            Assumes.Present(textManager);
            var componentModel = (SComponentModel)await _serviceProvider.GetServiceAsync(typeof(SComponentModel));
            Assumes.Present(componentModel);

            IVsTextView view;
            IWpfTextView wpfTextView;
            ITextEdit edit = null;

            try
            {
                int result = (textManager as IVsTextManager2).GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out view);
                _logger.Info("Pomyślnie utworzono obiekt IVsTextView");
                // Przekonwertuj IVsTextView na IWpfTextView, aby móc modyfikować tekst pliku (kod)
                wpfTextView = (componentModel as IComponentModel).GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(view);
                _logger.Info("Pomyślnie utworzono obiekt IWpfTextView");
                // Ważny moment: tutaj tworzymy obiekt do edycji tekstu kodu
                edit = wpfTextView.TextBuffer.CreateEdit();
                _logger.Info("Pomyślnie wykonano CreateEdit() oraz utworzono obiekt do edycji tekstu");

                IVsHierarchy hierarchy = null;
                string csProjPath = null;

                var selection = wpfTextView.Selection;

                var startPosition = selection.Start.Position.Position;
                var endPosition = selection.End.Position.Position;
                var span = new Span(startPosition, endPosition - startPosition);

                var textToReplace = wpfTextView.TextBuffer.CurrentSnapshot.GetText(span).Trim(' ').Trim('"');

                var vm = new AddTranslationViewModel(new ProjectItemsFactory(_serviceProvider));
                var translationWindow = new AddTranslationWindow(vm);

                translationWindow.ShowDialog();

                if (translationWindow.DialogResult != null && !translationWindow.DialogResult.Value)
                {
                    _logger.Info("Anulowano dodawanie tłumaczenia!");
                    return;
                }
#warning TO UNCOMMENT
                // edit.Replace(span, translationWindow.TranslationName);
                edit.Apply();
            }
            catch (InvalidOperationException ioe)
            {
                string err = $"Wystąpił błąd!\n{ioe.ToString()}";
                MessageBox.Show("Aby nie utracić wprowadzonych zmian NIE NALEŻY nic klikać," +
                  " ponieważ spowoduje to zawieszenie Visuala i zmiany zostaną utracone." +
                  " Należy zapisać plik, zamknąć go i ponownie otworzyć.");
                _logger.Info(ioe.ToString());
                if (ioe.InnerException != null)
                    _logger.Info(ioe.InnerException.ToString());
                return;
            }
            catch (Exception ex)
            {
                string err = $"Wystąpił błąd!\n{ex.ToString()}";
                MessageBox.Show(err);
                _logger.Info(err);
                _logger.Info(ex.ToString());
                if (ex.InnerException != null)
                    _logger.Info(ex.InnerException.ToString());
                return;
            }
            finally
            {
                edit?.Dispose();
            }
        }
    }
}
