using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnippetMasterWPF.Views.Pages;
using SnippetMasterWPF.Views.Windows;
using Wpf.Ui;

namespace SnippetMasterWPF.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private INavigationWindow _navigationWindow;
        private ClipboardHistoryWindow? _clipboardWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
            
            // Start clipboard monitoring
            var clipboardService = _serviceProvider.GetService<SnipMasterLib.Services.IClipboardService>();
            clipboardService?.StartMonitoring();
            
            // Register clipboard history hotkey
            var hotKeyService = _serviceProvider.GetService<IHotKeyService>();
            hotKeyService?.RegisterClipboardHistoryHotkey(ShowClipboardHistory);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop clipboard monitoring
            var clipboardService = _serviceProvider.GetService<SnipMasterLib.Services.IClipboardService>();
            clipboardService?.StopMonitoring();
            
            await Task.CompletedTask;
        }
        
        private void ShowClipboardHistory()
        {
            if (_clipboardWindow?.IsVisible == true)
            {
                _clipboardWindow.Close();
                _clipboardWindow = null;
            }
            else
            {
                _clipboardWindow?.Close();
                _clipboardWindow = _serviceProvider.GetService<Views.Windows.ClipboardHistoryWindow>();
                _clipboardWindow?.Show();
            }
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (
                    _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;
                _navigationWindow!.ShowWindow();

                _navigationWindow.Navigate(typeof(Views.Pages.DashboardPage));
            }

            await Task.CompletedTask;
        }
    }
}
