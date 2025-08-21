using Wpf.Ui;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Services;

public class NotificationService : INotificationService
{
    private readonly ISnackbarService _snackbarService;

    public NotificationService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    public void ShowNotification(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Info,
            null,
            TimeSpan.FromSeconds(3)
        );
    }
}