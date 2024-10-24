using Microsoft.Extensions.DependencyInjection;
using SnippetMasterWPF.Infrastructure.Mvvm;
using SnippetMasterWPF.Models;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using RelayCommand = SnippetMasterWPF.Infrastructure.Mvvm.RelayCommand;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class DataViewModel() : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        internal DashboardViewModel _dashboardViewModel;

        private IDiffView _diffView;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        public IDiffView DiffView
        {
            get => _diffView; 
            set
            { 
                _diffView = value; 
            }
        }

        private string _original = String.Empty;

        public string Original
        {
            get => _original;
            set 
            { 
                _original = value;
                OnPropertyChanged();
            }
        }

        private string _latest = String.Empty;

        public string Latest
        {
            get  => _latest ;
            set 
            { 
                _latest  = value;
                OnPropertyChanged();
            }
        }

        private SymbolIcon _uploadBtnIcon = new SymbolIcon { Symbol = SymbolRegular.FolderSwap16 };

        public SymbolIcon UploadBtnIcon
        {
            get { return _uploadBtnIcon; }
            set { _uploadBtnIcon = value; }
        }

        private SymbolIcon _insertTextOriIcon = new SymbolIcon {  Symbol = SymbolRegular.DocumentArrowLeft24 };

        public SymbolIcon InsertTextOriIcon
        {
            get { return _insertTextOriIcon; }
            set { _insertTextOriIcon = value; }
        }


        private SymbolIcon _insertTextLatIcon = new SymbolIcon {  Symbol = SymbolRegular.DocumentArrowRight24 };

        public SymbolIcon InsertTextLatIcon
        {
            get { return _insertTextLatIcon; }
            set { _insertTextLatIcon = value; }
        }


        private SymbolIcon _clearTextIcon = new SymbolIcon {  Symbol = SymbolRegular.CalendarCancel24 };

        public SymbolIcon ClearTextIcon
        {
            get => _clearTextIcon;
            set 
            { 
                _clearTextIcon = value; 
            }
        }


        //commands
        public ICommand UploadDiffFileCommand => new RelayCommand(UploadDiffFile);
        public ICommand InsertTextLatestCommand => new RelayCommand(InsertTextLatest, CanInsertTextLatest);
        public ICommand InsertTextOriginalCommand => new RelayCommand(InsertTextOriginal, CanInsertTextOriginal);
        public ICommand ClearPanelsCommand => new RelayCommand(ClearPanels);

        //methods
        private  void UploadDiffFile()
        {
            DiffView.ShowOpenFileContextMenu();
        }

        private void InsertTextOriginal()
        {
            Original = _dashboardViewModel.SnippetText;
        }

        private bool CanInsertTextOriginal()
        {
            return !string.IsNullOrEmpty(_dashboardViewModel.SnippetText);
        }

        private void InsertTextLatest()
        {
            Latest = _dashboardViewModel.SnippetText;
        }

        private bool CanInsertTextLatest()
        {
            return !string.IsNullOrEmpty(_dashboardViewModel.SnippetText);
        }

        private void ClearPanels()
        {
            DiffView.ClearPanels();
        }
    }
}
