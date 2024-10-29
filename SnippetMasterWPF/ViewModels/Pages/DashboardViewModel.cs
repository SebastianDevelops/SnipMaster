using SnippetMasterWPF.Services;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;
using RelayCommand = SnippetMasterWPF.Infrastructure.Mvvm.RelayCommand;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
		private readonly ITesseractService _tesseractService;
		private readonly ISnippingService _snippingService;
        private readonly IHotKeyService _hotKeyService;
        private readonly IDeviceLinkService _deviceLinkService;
        private readonly IContentDialogService _contentDialogService;

        public DashboardViewModel(ITesseractService tesseractService, 
                                  ISnippingService snippingService,
                                  IHotKeyService hotKeyService,
                                  IDeviceLinkService deviceLinkService,
                                  IContentDialogService contentDialogService)
        {
			_tesseractService = tesseractService ?? throw new NullReferenceException();
            _snippingService = snippingService ?? throw new NullReferenceException();
            _hotKeyService = hotKeyService ?? throw new NullReferenceException();
            _deviceLinkService = deviceLinkService ?? throw new NullReferenceException();
            _contentDialogService = contentDialogService ?? throw new NullReferenceException();

            _snippingService.OnSnipCompleted += OnSnipCompleted;
            _hotKeyService.RegisterHotkeys(StartSnipping);
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
        public ICommand SendToDeviceCommand => new RelayCommand(async () => await SendToDevice(), CanSendToDevice);

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

		private void StartSnipping()
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

        private async Task SendToDevice()
        {
            var qrCode = await _deviceLinkService.GenerateDeviceLink(SnippetText);

            if(qrCode.Value == null)
            {
                return;
            }

            ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions()
            {
                Title = "Scan Qr code to download text on device",
                Content = qrCode.Value,
                CloseButtonText = "Close",
            }
        );
        }

        private bool CanSendToDevice()
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
                    SnippetText = snippetText;
            }
			catch (Exception ex)
			{
                MessageBox.Show($@"Something went wrong while reading file: {Environment.NewLine}{ex.Message}"
                                                 , "Error");
            }
           
        }
    }
}
