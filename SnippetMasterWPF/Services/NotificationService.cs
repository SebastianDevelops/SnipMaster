using System.Drawing;
using System.Windows.Forms;

namespace SnippetMasterWPF.Services;

public class NotificationService : INotificationService
{
    private readonly NotifyIcon _notifyIcon;

    public NotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = false
        };
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        
        // Hide immediately after showing balloon tip
        Task.Delay(100).ContinueWith(_ => 
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                _notifyIcon.Visible = false;
            });
        });
    }
}