using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;

namespace SnipMasterLib.Helpers
{
    public static class ImageConversionHelper
    {
        /// <summary>
        /// Converts a System.Drawing.Bitmap to a WPF-compatible BitmapImage.
        /// </summary>
        /// <param name="image">The System.Drawing.Bitmap to convert.</param>
        /// <returns>A BitmapImage object that can be used in WPF controls.</returns>
        public static BitmapImage ConvertToBitmapImage(Bitmap image)
        {
            using (var memory = new MemoryStream())
            {
                // Save the bitmap to a memory stream in PNG format
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                // Create a BitmapImage and load it from the memory stream
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
