using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace SnipMaster
{
    public class SnippingForm : Form
    {
        private Point startPoint;
        private Point endPoint;
        private bool isDragging;
        private Rectangle selectionRectangle;

        /// <summary>
        /// Gets the rectangle representing the selected area.
        /// </summary>
        public Rectangle SelectionRectangle => selectionRectangle;
    }
}
