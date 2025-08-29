using System.Configuration;
using System.IO;
using System.Windows.Media.Imaging;
using SnippetMasterWPF.Helpers;
using Syncfusion.Pdf.Graphics;
using Syncfusion.OCRProcessor;
using Syncfusion.Pdf.Graphics;
using System.Drawing;

namespace SnippetMasterWPF.Services
{
    public class TesseractService : ITesseractService
    {
        public TesseractService()
        {
            var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
                           ?? ConfigurationManager.AppSettings["SyncfusionLicenseKey"];
            
            if (!string.IsNullOrEmpty(licenseKey))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
        }
        
        public string ReadFromUploadedFile(string filePath)
        {
            using (var processor = new OCRProcessor())
            using (var bitmap = new Bitmap(filePath))
            {
                processor.Settings.Language = Languages.English;
                string text = processor.PerformOCR(bitmap, @"tessdata/").ToString();

                if (String.IsNullOrEmpty(text))
                {
                    MessageBox.Show("Something went wrong while reading the file");
                }

                return text;
            }
        }

        public string ReadFromSnippedImage(BitmapImage image)
        {
            using (var memoryStream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                
                using (var bitmap = new Bitmap(memoryStream))
                using (var processor = new OCRProcessor())
                {
                    processor.Settings.Language = Languages.English;
                    string ocrText = processor.PerformOCR(bitmap, @"tessdata/").ToString();

                    if (String.IsNullOrEmpty(ocrText))
                    {
                        MessageBox.Show("Something went wrong while reading the image");
                    }
                    
                    return ocrText;
                }
            }
        }
    }

    public interface ITesseractService
    {
        string ReadFromUploadedFile(string filePath);
        string ReadFromSnippedImage(BitmapImage image);
    }
}
