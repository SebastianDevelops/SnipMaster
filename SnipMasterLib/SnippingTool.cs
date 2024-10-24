using System;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using Image = System.Drawing.Image;
using SnipMasterLib.Helpers;
using WinFormsApp1;
using System.Windows.Media.Imaging;

namespace SnipMasterLib
{
    public class SnippingTool
    {
        /// <summary>
        /// Event that is triggered when the user completes a snip.
        /// </summary>
        public event Action<BitmapImage> OnSnipCompleted;

        private Rectangle _captureRectangle;
        private Bitmap _snippedImage;

        /// <summary>
        /// Starts the snipping tool to capture a section of the screen.
        /// </summary>
        public void StartSnipping()
        {
            using (var snipForm = new SnippingForm())
            {
                if (snipForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _captureRectangle = snipForm.SelectionRectangle;
                    _snippedImage = ScreenCaptureHelper.CaptureScreen(_captureRectangle)!;

                    if(_snippedImage is null)
                    {
                        return;
                    }
                    // Convert the captured Bitmap (System.Drawing.Image) to BitmapImage (WPF compatible)
                    var bitmapImage = ImageConversionHelper.ConvertToBitmapImage(_snippedImage);

                    // Trigger the callback with the converted image
                    OnSnipCompleted?.Invoke(bitmapImage);
                }
            }
        }

        /// <summary>
        /// Saves the captured snip to a file.
        /// </summary>
        /// <param name="filePath">The file path where the image will be saved.</param>
        public void SaveSnip(string filePath)
        {
            _snippedImage?.Save(filePath);
        }

        /// <summary>
        /// Returns the captured snip as an image.
        /// </summary>
        /// <returns>The snipped image as a Bitmap.</returns>
        public Bitmap GetSnippedImage()
        {
            return _snippedImage;
        }
    }
}
