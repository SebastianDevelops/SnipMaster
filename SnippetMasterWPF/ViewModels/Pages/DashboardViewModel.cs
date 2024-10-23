using SnippetMasterWPF.Services;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
		private readonly ITesseractService _tesseractService;
		private readonly ISnippingService _snippingService;

        public DashboardViewModel(ITesseractService tesseractService, ISnippingService snippingService)
        {
			_tesseractService = tesseractService ?? throw new NullReferenceException();
            _snippingService = snippingService ?? throw new NullReferenceException();

            _snippingService.OnSnipCompleted += OnSnipCompleted;
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
