using AddTranslation.Core;
using AddTranslation.Vsix.Vsix;
using AddTranslation.Vsix.Windows;
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
using System.ComponentModel.Design;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace AddTranslation.Vsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddTranslationCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(nameof(AddTranslationCommand));

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
#warning Try to find out better solution, this seems like a workaround.
            // For some reason, not all DLLs are loaded in extension (this how VS experimental instance loads them),
            // so here we manually load assembly and then resources.
            try
            {
                System.Reflection.Assembly.Load("AddTranslation.Core");

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/ColorsAndBrushes.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/SmallStylesForControls.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/Button.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/ScrollBar.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/ComboBox.xaml", UriKind.RelativeOrAbsolute)
                });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(@"pack://application:,,,/AddTranslation.Core;component/UserInterfaceResources/DataGridStylesAndTemplates.xaml", UriKind.RelativeOrAbsolute)
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not load AddTranslation.Core assembly.", ex);
                MessageBox.Show($"Could not load AddTranslation.Core assembly.\nException: {ex.Message}\nPlease report this at GitHub repo https://github.com/mturczyn/TranslationExtension/issues", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            => await ExecuteAsync(sender, e);

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            _logger.Info("Starting command of adding translation.");

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
                _logger.Debug("Successfully created " + nameof(IVsTextView));
                // Cast IVsTextView to IWpfTextView, so we can modify text of the file.
                wpfTextView = (componentModel as IComponentModel).GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(view);
                _logger.Debug("Successfully created " + nameof(IWpfTextView));
                // IMPORTANT MOMENT: here we create ITextEdit object, which, in case of a failure,
                // may hang whole visual studio (thus, preventing from saving unsaved work,
                // see catch(InvalidOperationException)).
                edit = wpfTextView.TextBuffer.CreateEdit();
                _logger.Info($"Successfully executed {nameof(wpfTextView.TextBuffer.CreateEdit)} and created {nameof(ITextEdit)} object.");

                IVsHierarchy hierarchy = null;
                string csProjPath = null;

                var selection = wpfTextView.Selection;

                var startPosition = selection.Start.Position.Position;
                var endPosition = selection.End.Position.Position;
                var span = new Span(startPosition, endPosition - startPosition);

                var textToReplace = wpfTextView.TextBuffer.CurrentSnapshot.GetText(span).Trim(' ').Trim('"');

                var translationWindow = new AddTranslationWindow();
                var vm = new AddTranslationViewModel(new ProjectItemsFactory(_serviceProvider), translationWindow);
                translationWindow.DataContext = vm;

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
                MessageBox.Show("This should not happen, but it happened. Please, do not click anything," +
                    " just save the file in order to not loose any work, then close the file.\nError:\n" + ioe);
                _logger.Fatal("Fatal exception during translation command..", ioe);
                if (ioe.InnerException != null)
                    _logger.Fatal("Fatal inner exception.", ioe);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The translation extension reported an exception:\n{ex}");
                _logger.Error("Exception during translation command.", ex);
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
