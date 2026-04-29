using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CardGameScorer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string LogFileName = "crash_log.txt";
    private const int MaxLogEntries = 20; // Keep only the last 20 crash entries
    private const string EntrySeparator = "\n========================================\n";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Handle UI thread exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        LogException(args.Exception, "UI Thread Exception");
        args.Handled = false;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception ex)
        {
            LogException(ex, "Unhandled Exception");
        }
        else
        {
            LogException(new Exception(args.ExceptionObject?.ToString() ?? "Unknown error"), "Unhandled Exception");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        LogException(args.Exception, "Unobserved Task Exception");
        args.SetObserved();
    }

    private static void LogException(Exception exception, string source)
    {
        try
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFileName);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Build the new log entry
            string newEntry = $"[{timestamp}] {source}\n" +
                              $"Message: {exception.Message}\n" +
                              $"Type: {exception.GetType().FullName}\n" +
                              $"Stack Trace:\n{exception.StackTrace}";
            
            // Add inner exception if present
            if (exception.InnerException != null)
            {
                newEntry += $"\n\nInner Exception: {exception.InnerException.Message}\n" +
                           $"Inner Stack Trace:\n{exception.InnerException.StackTrace}";
            }

            // Read existing entries and apply FIFO
            List<string> entries = new List<string>();
            if (File.Exists(logPath))
            {
                string existingContent = File.ReadAllText(logPath);
                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    entries = existingContent.Split(new[] { EntrySeparator }, StringSplitOptions.RemoveEmptyEntries)
                                             .ToList();
                }
            }

            // Add new entry at the end
            entries.Add(newEntry);

            // Keep only the last MaxLogEntries (FIFO - remove oldest first)
            while (entries.Count > MaxLogEntries)
            {
                entries.RemoveAt(0);
            }

            // Write back to file
            string finalContent = string.Join(EntrySeparator, entries) + EntrySeparator;
            File.WriteAllText(logPath, finalContent);
        }
        catch (Exception logEx)
        {
            // Don't cause another exception while logging; surface to debugger only.
            System.Diagnostics.Debug.WriteLine($"Failed to write crash log: {logEx.Message}");
        }
    }
}

