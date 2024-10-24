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

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private void DataPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.DiffView = this;
                SetDiffViewer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization failed: {ex.Message}", "Error");
            }
        }

        private void SetDiffViewer()
        {
            CheckDiffView.ShowSideBySide();
            CheckDiffView.SetText("Text", "Test");
        }

        public void ShowOpenFileContextMenu()
        {
            CheckDiffView.ShowOpenFileContextMenu();
        }
    }
}
