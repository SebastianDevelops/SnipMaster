using System.Net.Mime;
using SnippetMasterWPF.Services;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;
using RelayCommand = SnippetMasterWPF.Infrastructure.Mvvm.RelayCommand;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;
using SnippetMasterWPF.Infrastructure.Editor;
using SnippetMasterWPF.Models.Editor;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
		private readonly ITesseractService _tesseractService;
		private readonly ISnippingService _snippingService;
        private readonly IHotKeyService _hotKeyService;
        private readonly IContentDialogService _contentDialogService;
        private EditorController? _editorController;

        public class LanguageItem
        {
            public string Name { get; set; }
            public EditorLanguage Language { get; set; }
        }

        public List<LanguageItem> Languages { get; } = Enum.GetValues<EditorLanguage>()
            .Select(lang => new LanguageItem { Name = lang.ToString(), Language = lang })
            .ToList();

        private LanguageItem _selectedLanguage;
        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                _ = _editorController?.SetLanguageAsync(value.Language);
            }
        }

        public DashboardViewModel(ITesseractService tesseractService, 
                                  ISnippingService snippingService,
                                  IHotKeyService hotKeyService,
                                  IContentDialogService contentDialogService)
        {
			_tesseractService = tesseractService ?? throw new NullReferenceException();
            _snippingService = snippingService ?? throw new NullReferenceException();
            _hotKeyService = hotKeyService ?? throw new NullReferenceException();
            _contentDialogService = contentDialogService ?? throw new NullReferenceException();

            _snippingService.OnSnipCompleted += OnSnipCompleted;
            _hotKeyService.RegisterHotkeys(StartSnipping);
            _selectedLanguage = Languages[0];
        }
        
        public void SetWebView(WebView2 webView)
        {
	        webView.NavigationCompleted += OnWebViewNavigationCompleted;
	        webView.SetCurrentValue(FrameworkElement.UseLayoutRoundingProperty, true);
	        webView.SetCurrentValue(WebView2.DefaultBackgroundColorProperty, System.Drawing.Color.Transparent);
	        webView.SetCurrentValue(
		        WebView2.SourceProperty,
		        new Uri(
			        System.IO.Path.Combine(
				        System.AppDomain.CurrentDomain.BaseDirectory,
				        @"Assets\Monaco\index.html"
			        )
		        )
	        );

	        _editorController = new EditorController(webView);
        }

        private string _snippetText = String.Empty;

		public string SnippetText
		{
			get => _snippetText;
			set 
			{ 
				_snippetText = value;
				OnPropertyChanged();
            }
		}

		private BitmapImage _snippedImage = new();

		public BitmapImage SnippedImage
		{
			get => _snippedImage; 
			set 
			{ 
				_snippedImage = value;
                OnPropertyChanged();
            }
		}


		//commands
		public ICommand UploadFileCommand => new RelayCommand(UploadFile);
		public ICommand SnipImageCommand => new RelayCommand(StartSnipping);
        public ICommand CopySnippetCommand => new RelayCommand(CopySnippet, CanCopySnippet);

        //methods
        public void UploadFile()
		{
			try
			{
                OpenFileDialog open = new OpenFileDialog();
                // image filters  
                open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string fileName = open.FileName;

                    string fileText = _tesseractService.ReadFromUploadedFile(fileName);

                    if (!string.IsNullOrEmpty(fileText))
                        SnippetText = fileText;
                }
            }
			catch (Exception ex)
			{
				MessageBox.Show($@"Something went wrong while reading file: {Environment.NewLine}{ex.Message}"
					             , "Error");
			}
			
		}

		public void StartSnipping()
        {
            _snippingService.StartSnipping();
        }

		private void CopySnippet()
		{
            try
            {
                Clipboard.SetDataObject(SnippetText);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to access the clipboard: " + ex.Message);
            }
        }

        private bool CanCopySnippet()
        {
            return !string.IsNullOrEmpty(SnippetText);
        }

		private void OnSnipCompleted(BitmapImage snippedImage)
        {
			try
			{
                if (snippedImage is null)
                {
                    MessageBox.Show("Something went wrong while snipping the image", "Error");
                    return;
                }

                string snippetText = _tesseractService.ReadFromSnippedImage(snippedImage);

                if (!string.IsNullOrEmpty(snippetText))
                {
                    SnippetText = snippetText;
                    _ = _editorController?.SetContentAsync(snippetText);
                    try
                    {
                        Clipboard.SetDataObject(SnippetText);
                    }
                    catch
                    {
                    }
                }
            }
			catch (Exception ex)
			{
                MessageBox.Show($@"Something went wrong while reading file: {Environment.NewLine}{ex.Message}"
                                                 , "Error");
            }
           
        }
		
        private async Task InitializeEditorAsync()
        {
	        if (_editorController == null)
	        {
		        return;
	        }

	        await _editorController.CreateAsync();
	        await _editorController.SetThemeAsync(ApplicationThemeManager.GetAppTheme());
	        await _editorController.SetLanguageAsync(EditorLanguage.Csharp);
	        await _editorController.SetContentAsync("");
        }
		
        private void OnWebViewNavigationCompleted(
	        object? sender,
	        Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e
        )
        {
	        _ = DispatchAsync(InitializeEditorAsync);
        }
        
        private static DispatcherOperation<TResult> DispatchAsync<TResult>(Func<TResult> callback)
        {
	        return System.Windows.Application.Current.Dispatcher.InvokeAsync(callback);
        }
    }
}
