using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace AddTranslation.LogService
{
  public static class Logger
  {
    private static StringBuilder _log = new StringBuilder();

    public static void AppendInfoLine(string line, 
      [CallerMemberName] string callerName = "",
      [CallerLineNumber] int sourceLineNumber = 0)
    {
      _log.AppendLine($"INFO {DateTime.Now} {callerName}: {sourceLineNumber} | {line}");
    }

    public static void AppendWarningLine(string line,
      [CallerMemberName] string callerName = "",
      [CallerLineNumber] int sourceLineNumber = 0)
    {
      _log.AppendLine($"WARNING {DateTime.Now} {callerName}: {sourceLineNumber} | {line}");
    }

    public static void AppendErrorLine(string line,
      [CallerMemberName] string callerName = "",
      [CallerLineNumber] int sourceLineNumber = 0)
    {
      _log.AppendLine($"ERROR {DateTime.Now} {callerName}: {sourceLineNumber} | {line}");
    }

    public static void AppendLine(string line)
    {
      _log.AppendLine(line);
    }

    public static void ClearLogger()
    {
      _log.Clear();
    }

    public static void SaveLogs()
    {
      try
      {
        var sfd = new SaveFileDialog();
        sfd.AddExtension = true;
        sfd.Filter = "Log file (*.log)|*.log";
        sfd.Title = "Zapisz logi rozszerzenia";
        if (sfd.ShowDialog() ?? false)
          File.WriteAllText(sfd.FileName, _log.ToString());
        ClearLogger();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Nie powiodło się zapisywanie logów: {ex.ToString()}");
      }
    }
  }
}
