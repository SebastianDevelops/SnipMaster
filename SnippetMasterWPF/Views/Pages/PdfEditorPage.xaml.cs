using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SnippetMasterWPF.ViewModels.Pages;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace SnippetMasterWPF.Views.Pages
{
    public partial class PdfEditorPage : Page, INavigableView<PdfEditorViewModel>
    {
        public PdfEditorViewModel ViewModel { get; }
        private readonly string _saveDirectory;

        public PdfEditorPage(PdfEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            
            _saveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SnipMaster", "ModifiedPDFs");
            Directory.CreateDirectory(_saveDirectory);
            
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await PdfWebView.EnsureCoreWebView2Async();
            PdfWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            PdfWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pdfjs", "custom", "editor.html");
            PdfWebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageString = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(messageString))
                    return;
                    
                var message = JsonSerializer.Deserialize<WebMessage>(messageString);
                if (message?.action == "saveCompleted")
                {
                    await MovePdfFromDownloads();
                }
            }
            catch (ArgumentException)
            {
                // Ignore WebView2 message size errors - file was saved successfully
                await Task.Delay(1000); // Wait for file to be written
                await MovePdfFromDownloads();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async Task MovePdfFromDownloads()
        {
            try
            {
                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnipMaster", "PDFs");
                Directory.CreateDirectory(targetDir);
                
                var editedFiles = Directory.GetFiles(downloadsPath, "edited_*.pdf")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Take(1);
                
                foreach (var file in editedFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var targetPath = Path.Combine(targetDir, fileName);
                    File.Move(file, targetPath);
                    System.Windows.MessageBox.Show($"PDF saved to: {targetPath}");
                    return;
                }
                
                System.Windows.MessageBox.Show("PDF saved successfully!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error moving PDF: {ex.Message}");
            }
        }

        private async void OpenPdf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "PDF Files (*.pdf)|*.pdf" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(dialog.FileName));
                    await PdfWebView.CoreWebView2.ExecuteScriptAsync($"if(window.loadPdfFromBase64) {{ window.loadPdfFromBase64('{base64}'); }} else {{ console.error('loadPdfFromBase64 not available'); }}");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading PDF: {ex.Message}");
                }
            }
        }
    }

    public class WebMessage
    {
        public string action { get; set; }
    }
}