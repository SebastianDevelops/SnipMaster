using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace SnippetMasterWPF.Services;

public class LogService : INotifyPropertyChanged
{
    public static LogService Instance { get; } = new LogService();
    public ObservableCollection<string> Messages { get; } = new();

    private LogService() { }

    public void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}: {message}");
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;
}