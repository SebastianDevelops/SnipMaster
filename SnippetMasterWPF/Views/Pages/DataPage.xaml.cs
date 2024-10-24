using Microsoft.Extensions.DependencyInjection;
using SnippetMasterWPF.Infrastructure.Mvvm;
using SnippetMasterWPF.ViewModels.Pages;
using SnippetMasterWPF.ViewModels.Windows;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace SnippetMasterWPF.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>, IDiffView
    {
        public DataViewModel ViewModel { get; }
        public IServiceProvider _serviceProvider { get; set; }

        public DataPage(DataViewModel viewModel, IServiceProvider serviceProvider)
        {
            ViewModel = viewModel;
            DataContext = this;
            _serviceProvider = serviceProvider ?? throw new NullReferenceException();

            ViewModel.DiffView = this;
            ViewModel._dashboardViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>() ?? throw new NullReferenceException();

            InitializeComponent();

            SetDiffViewer();
        }

        private void SetDiffViewer()
        {
            CheckDiffView.ShowSideBySide();
        }

        public void ShowOpenFileContextMenu()
        {
            CheckDiffView.ShowOpenFileContextMenu();
        }

        public void ClearPanels()
        {
            CheckDiffView.OldText = String.Empty;
            CheckDiffView.NewText = String.Empty;
        }
    }
}
