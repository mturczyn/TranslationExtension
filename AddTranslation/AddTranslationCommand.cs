using AddTranslation.LogService;
using AddTranslation.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace AddTranslation
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class AddTranslationCommand
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("52faa2b6-add8-48ca-89c4-e2487d1d625b");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly AsyncPackage package;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTranslationCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private AddTranslationCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
      this.package = package ?? throw new ArgumentNullException(nameof(package));
      commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

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
    /// Gets the service provider from the owner package.
    /// </summary>
    private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
    {
      get
      {
        return this.package;
      }
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

      OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
      Instance = new AddTranslationCommand(package, commandService);
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

      var textManager = ServiceProvider.GetServiceAsync(typeof(SVsTextManager));
      var componentModel = this.ServiceProvider.GetServiceAsync(typeof(SComponentModel));
      IVsTextView view;
      IWpfTextView wpfTextView;
      ITextEdit edit = null;
      
      try
      {
        int result = ((await textManager) as IVsTextManager2).GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out view);
        Logger.AppendInfoLine("Pomyślnie utworzono obiekt IVsTextView");
        // Przekonwertuj IVsTextView na IWpfTextView, aby móc modyfikować tekst pliku (kod)
        wpfTextView = (((await componentModel) as IComponentModel).GetService<IVsEditorAdaptersFactoryService>()).GetWpfTextView(view);
        Logger.AppendInfoLine("Pomyślnie utworzono obiekt IWpfTextView");
        // Ważny moment: tutaj tworzymy obiekt do edycji tekstu kodu
        edit = wpfTextView.TextBuffer.CreateEdit();
        Logger.AppendInfoLine("Pomyślnie wykonano CreateEdit() oraz utworzono obiekt do edycji tekstu");
        
        IVsHierarchy hierarchy = null;
        string csProjPath = null;
        // uint itemid = VSConstants.ALL;//.VSITEMID_NIL;
        if (IsSingleProjectItemSelection(out hierarchy, out uint itemid))
        {
          Logger.AppendInfoLine("IsSingleProjectItemSelection zwróciło true...");
          IVsProject project;
          if ((project = hierarchy as IVsProject) != null)
          {
            Logger.AppendInfoLine("Pobieranie ścieżki do projektu.");
            // Pobieramy ścieżkę do pliku csproj
            project.GetMkDocument(VSConstants.VSITEMID_ROOT, out csProjPath);

            Logger.AppendInfoLine("Pobrano ścieżkę do projektu: " + csProjPath ?? "NULL");

            if((!csProjPath?.EndsWith(".csproj")) ?? true)
            {
              MessageBox.Show("Nie udało się zlokalizować pliku projektu csproj", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
              Logger.SaveLogs();
              Logger.ClearLogger();
              return;
            }
          }
        }

        var selection = wpfTextView.Selection;

        var startPosition = selection.Start.Position.Position;
        var endPosition = selection.End.Position.Position;
        var span = new Span(startPosition, endPosition - startPosition);
        
        var textToReplace = wpfTextView.TextBuffer.CurrentSnapshot.GetText(span).Trim(' ').Trim('"');

        var translationWindow = new AddTranslationWindow(textToReplace, csProjPath, out bool shouldNotOpenTheWindow);
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
        textManager?.Dispose();
        Logger.ClearLogger();
      }
    }
    #region Metody pomocnicze
    public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
    {
      hierarchy = null;
      itemid = VSConstants.ALL;//.VSITEMID_NIL;
      int hr = VSConstants.S_OK;
      var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
      var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
      if (monitorSelection == null || solution == null) return false;

      IVsMultiItemSelect multiItemSelect = null;
      IntPtr hierarchyPtr = IntPtr.Zero;
      IntPtr selectionContainerPtr = IntPtr.Zero;

      try
      {
        hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);
        // if there is no selection
        if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.ALL) //VSITEMID_NIL          
          return false;
        // multiple items are selected
        if (multiItemSelect != null) return false;
        // there is a hierarchy root node selected, thus it is not a single item inside a project
        if (itemid == VSConstants.VSITEMID_ROOT) return false;

        hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
        if (hierarchy == null) return false;

        Guid guidProjectID = Guid.Empty;
        if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
          return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid

        // if we got this far then there is a single project item selected
        return true;
      }
      finally
      {
        if (selectionContainerPtr != IntPtr.Zero)
          Marshal.Release(selectionContainerPtr);

        if (hierarchyPtr != IntPtr.Zero)
          Marshal.Release(hierarchyPtr);
      }
    }
    #endregion
  }
}
