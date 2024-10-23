using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace SnipMasterLib.Helpers
{
    public static class ScreenCaptureHelper
    {
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(nint hdcDest, int xDest, int yDest, int wDest, int hDest,
                                  nint hdcSrc, int xSrc, int ySrc, int rop);

        /// <summary>
        /// Captures a section of the screen based on the provided rectangle.
        /// </summary>
        /// <param name="rect">The rectangle representing the area to capture.</param>
        /// <returns>A Bitmap object containing the captured screen section.</returns>
        public static Bitmap CaptureScreen(Rectangle rect)
        {
            Bitmap screenshot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }
            return screenshot;
        }
    }
}
