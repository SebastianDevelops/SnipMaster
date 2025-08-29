using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message);
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
    void ShowWarning(string title, string message);
    void ShowInfo(string title, string message);
}