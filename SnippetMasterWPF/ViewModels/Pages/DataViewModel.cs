using SnippetMasterWPF.Infrastructure.Mvvm;
using SnippetMasterWPF.Models;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private IDiffView _diffView;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            var test = InfoBadgeSeverity.Informational;

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


    }
}
