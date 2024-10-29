using SnippetMasterWPF.Helpers.Hotkeys;
using SnippetMasterWPF.ViewModels.Pages;
using SnippetMasterWPF.ViewModels.Windows;
using SnippetMasterWPF.Views.Pages;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;

namespace SnippetMasterWPF.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }
        private readonly NotifyIcon _trayIcon;

        public MainWindow(
            MainWindowViewModel viewModel,
            DashboardViewModel dashboardViewModel,
            IPageService pageService,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);
            HotKeysManager.SetupSystemHook();

            _trayIcon = new NotifyIcon();

            _trayIcon.Icon = new System.Drawing.Icon("snippet-master-icon.ico");
            _trayIcon.Text = "Snippet Master";
            _trayIcon.Visible = true;

            _trayIcon.ContextMenuStrip = new ContextMenuStrip();
            _trayIcon.ContextMenuStrip.Items.Add("Snip", null, (sender, args) => dashboardViewModel.StartSnipping());
            _trayIcon.ContextMenuStrip.Items.Add("Open", null, (sender, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            });
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (sender, args) => this.Close());

            _trayIcon.DoubleClick +=
                delegate (object? sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            _trayIcon.Dispose();
            HotKeysManager.ShutdownSystemHook();
            Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                _trayIcon.ShowBalloonTip(3000, "Snippet Master", "The app is still running.", ToolTipIcon.Info);
            }


            base.OnStateChanged(e);
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}
