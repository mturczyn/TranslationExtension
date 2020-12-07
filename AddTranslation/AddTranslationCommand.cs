using AddTranslation.LogService;
using AddTranslation.Windows;
using EnvDTE;
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

        private async Task<List<VSLangProj.Reference>> GetProjectReferencesForCUrrentlyOpenedFileAsync()
        {
            var dte = (DTE)await _serviceProvider.GetServiceAsync(typeof(DTE));
            Assumes.Present(dte);
            var vsProject = dte.ActiveDocument.ProjectItem.ContainingProject.Object as VSLangProj.VSProject;
            if (vsProject == null)
            {
                // Cant get the current project.
                return null;
            }

            var projectReferences = new List<VSLangProj.Reference>();
            foreach (VSLangProj.Reference @ref in vsProject.References)
            {
                if (@ref.SourceProject != null)
                    projectReferences.Add(@ref);
            }

            return projectReferences;
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
            Logger.AppendInfoLine("Rozpoczęcie tłumaczenia");

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
                Logger.AppendInfoLine("Pomyślnie utworzono obiekt IVsTextView");
                // Przekonwertuj IVsTextView na IWpfTextView, aby móc modyfikować tekst pliku (kod)
                wpfTextView = (componentModel as IComponentModel).GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(view);
                Logger.AppendInfoLine("Pomyślnie utworzono obiekt IWpfTextView");
                // Ważny moment: tutaj tworzymy obiekt do edycji tekstu kodu
                edit = wpfTextView.TextBuffer.CreateEdit();
                Logger.AppendInfoLine("Pomyślnie wykonano CreateEdit() oraz utworzono obiekt do edycji tekstu");

                IVsHierarchy hierarchy = null;
                string csProjPath = null;

                var projectReferences = await GetProjectReferencesForCUrrentlyOpenedFileAsync();

                var selection = wpfTextView.Selection;

                var startPosition = selection.Start.Position.Position;
                var endPosition = selection.End.Position.Position;
                var span = new Span(startPosition, endPosition - startPosition);

                var textToReplace = wpfTextView.TextBuffer.CurrentSnapshot.GetText(span).Trim(' ').Trim('"');


                var translationWindow = new AddTranslationWindow(textToReplace, csProjPath, projectReferences, out bool shouldNotOpenTheWindow);
                if (shouldNotOpenTheWindow)
                    // Jak musimy przeładować projekt, to nie pokazujemy okna w ogóle.
                    return;

                translationWindow.ShowDialog();

                if (translationWindow.DialogResult != null && !translationWindow.DialogResult.Value)
                {
                    Logger.AppendInfoLine("Anulowano dodawanie tłumaczenia!");
                    return;
                }

                edit.Replace(span, translationWindow.TranslationName);
                edit.Apply();
            }
            catch (InvalidOperationException ioe)
            {
                string err = $"Wystąpił błąd!\n{ioe.ToString()}";
                MessageBox.Show("Aby nie utracić wprowadzonych zmian NIE NALEŻY nic klikać," +
                  " ponieważ spowoduje to zawieszenie Visuala i zmiany zostaną utracone." +
                  " Należy zapisać plik, zamknąć go i ponownie otworzyć.");
                Logger.AppendErrorLine(ioe.ToString());
                if (ioe.InnerException != null)
                    Logger.AppendErrorLine(ioe.InnerException.ToString());
                Logger.SaveLogs();
                return;
            }
            catch (Exception ex)
            {
                string err = $"Wystąpił błąd!\n{ex.ToString()}";
                MessageBox.Show(err);
                Logger.AppendErrorLine(err);
                Logger.AppendErrorLine(ex.ToString());
                if (ex.InnerException != null)
                    Logger.AppendErrorLine(ex.InnerException.ToString());
                Logger.SaveLogs();
                return;
            }
            finally
            {
                edit?.Dispose();
                Logger.ClearLogger();
            }
        }
    }
}
