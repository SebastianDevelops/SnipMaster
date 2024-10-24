using System.IO;
using System.Windows.Media.Imaging;
using Tesseract;

namespace SnippetMasterWPF.Services
{
    public class TesseractService : ITesseractService
    {
        public string ReadFromUploadedFile(string filePath)
        {
            using (var ocrEngine = new TesseractEngine(@".\tessdata", "eng", EngineMode.Default))
            {
                var file = Pix.LoadFromFile(filePath);
                
                var result = ocrEngine.Process(file);

                if (result is null)
                {
                    MessageBox.Show("Something went wrong while reading the file");
                }

                return result!.GetText();
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

                using (var ocrEngine = new TesseractEngine(@".\tessdata", "eng", EngineMode.Default))
                {
                    var pix = Pix.LoadFromMemory(memoryStream.ToArray());
                    var result = ocrEngine.Process(pix);

                    if (result is null)
                    {
                        MessageBox.Show("Something went wrong while reading the image");
                    }

                    return result!.GetText();
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
