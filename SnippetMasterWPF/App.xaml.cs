using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnippetMasterWPF.Infrastructure.Mvvm;
using SnippetMasterWPF.Services;
using SnippetMasterWPF.ViewModels.Pages;
using SnippetMasterWPF.ViewModels.Windows;
using SnippetMasterWPF.Views.Pages;
using SnippetMasterWPF.Views.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;

namespace SnippetMasterWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                // Page resolver service
                services.AddSingleton<IPageService, PageService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IHotKeyService, HotKeyService>();
                services.AddTransient<ITesseractService, TesseractService>();
                services.AddTransient<ISnippingService, SnippingService>();
                services.AddSingleton<IScreenshotGeneratorService, ScreenshotGeneratorService>();
                services.AddHttpClient();
                services.AddTransient<IApiClient, ApiClient>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<SnipMaster.Compression.Services.ICompressionService, SnipMaster.Compression.Services.CompressionService>();
                services.AddSingleton<SnipMaster.Compression.Services.IMediaCompressionService, SnipMaster.Compression.Services.MediaCompressionService>();
                services.AddSingleton<SnipMaster.Conversion.Services.IFileConversionService, SnipMaster.Conversion.Services.FileConversionService>();
                services.AddSingleton<SnipMaster.Conversion.Services.IImageConversionService, SnipMaster.Conversion.Services.ImageConversionService>();
                services.AddSingleton<SnipMaster.Conversion.Services.IDocumentConversionService, SnipMaster.Conversion.Services.DocumentConversionService>();
                services.AddSingleton<SnipMaster.Conversion.Services.IConversionServiceFactory, SnipMaster.Conversion.Services.ConversionServiceFactory>();
                services.AddSingleton<SnipMaster.Conversion.Services.IUniversalConversionService, SnipMaster.Conversion.Services.UniversalConversionService>();
                services.AddSingleton<SnipMasterLib.Services.IClipboardService, SnipMasterLib.Services.ClipboardService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddTransient<IDiffView, DataPage>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<QuickTextActionsPage>();
                services.AddSingleton<QuickTextActionsViewModel>();
                services.AddSingleton<ClipboardHistoryPage>();
                services.AddSingleton<DocumentCompressionPage>();
                services.AddSingleton<DocumentCompressionViewModel>();
                services.AddSingleton<MediaCompressionPage>();
                services.AddSingleton<MediaCompressionViewModel>();
                services.AddSingleton<FileConversionPage>();
                services.AddSingleton<FileConversionViewModel>();
                services.AddSingleton<PdfEditorAiPage>();
                services.AddSingleton<PdfEditorAiViewModel>();
                services.AddTransient<ClipboardHistoryWindow>();
                services.AddTransient<ClipboardHistoryWindowViewModel>();
            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.Start();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
