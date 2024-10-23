using System;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using Image = System.Drawing.Image;

namespace SnipMaster
{
        public class SnippingTool
        {
            /// <summary>
            /// Event that is triggered when the user completes a snip.
            /// </summary>
            public event Action<Image> OnSnipCompleted;

            private Rectangle _captureRectangle;
            private Bitmap _snippedImage;

            /// <summary>
            /// Starts the snipping tool to capture a section of the screen.
            /// </summary>
            public void StartSnipping()
            {
                using (var snipForm = new SnippingForm())
                {
                    if (snipForm.ShowDialog() == DialogResult.OK)
                    {
                        _captureRectangle = snipForm.SelectionRectangle;
                        _snippedImage = ScreenCaptureHelper.CaptureScreen(_captureRectangle);
                        OnSnipCompleted?.Invoke(_snippedImage);
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
