namespace WinFormsApp1
{
    public partial class SnippingForm : Form
    {
        public SnippingForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        private Point startPoint;
        private Point endPoint;
        private bool isDragging;
        private Rectangle selectionRectangle;
        private readonly Color accentColor = Color.FromArgb(0, 120, 215); // Modern blue accent
        private readonly Font instructionFont = new Font("Segoe UI", 12, FontStyle.Regular);

        /// <summary>
        /// Gets the rectangle representing the selected area.
        /// </summary>
        public Rectangle SelectionRectangle => selectionRectangle;

        /// <summary>
        /// Event handler for mouse down event, starts the selection process.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            startPoint = e.Location;
            isDragging = true;
        }

        /// <summary>
        /// Event handler for mouse move event, updates the selection rectangle.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                endPoint = e.Location;
                Invalidate();  // Redraws the form to update the rectangle.
            }
        }

        /// <summary>
        /// Event handler for mouse up event, completes the selection process.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isDragging)
            {
                endPoint = e.Location;
                isDragging = false;
                selectionRectangle = GetSelectionRectangle();
                DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Paints the selection rectangle on the form with modern styling.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            
            if (!isDragging)
            {
                // Draw instruction text when not selecting
                using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                {
                    var text = "Click and drag to select an area to capture";
                    var textSize = e.Graphics.MeasureString(text, instructionFont);
                    var textRect = new RectangleF(
                        (Width - textSize.Width) / 2,
                        (Height - textSize.Height) / 2,
                        textSize.Width,
                        textSize.Height);
                    
                    e.Graphics.DrawString(text, instructionFont, brush, textRect);
                }
            }
            else
            {
                var rect = GetSelectionRectangle();
                
                // Draw selection area with modern styling
                using (var borderPen = new Pen(accentColor, 2))
                using (var shadowPen = new Pen(Color.FromArgb(100, 0, 0, 0), 1))
                using (var highlightBrush = new SolidBrush(Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B)))
                {
                    // Draw shadow
                    var shadowRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width, rect.Height);
                    e.Graphics.DrawRectangle(shadowPen, shadowRect);
                    
                    // Fill selection area with subtle highlight
                    e.Graphics.FillRectangle(highlightBrush, rect);
                    
                    // Draw main border
                    e.Graphics.DrawRectangle(borderPen, rect);
                    
                    // Draw corner handles
                    DrawCornerHandles(e.Graphics, rect);
                    
                    // Draw dimensions text
                    DrawDimensionsText(e.Graphics, rect);
                }
            }
        }
        
        private void DrawCornerHandles(Graphics g, Rectangle rect)
        {
            const int handleSize = 8;
            using (var handleBrush = new SolidBrush(accentColor))
            using (var handlePen = new Pen(Color.White, 1))
            {
                var handles = new Point[]
                {
                    new Point(rect.Left - handleSize/2, rect.Top - handleSize/2),
                    new Point(rect.Right - handleSize/2, rect.Top - handleSize/2),
                    new Point(rect.Left - handleSize/2, rect.Bottom - handleSize/2),
                    new Point(rect.Right - handleSize/2, rect.Bottom - handleSize/2)
                };
                
                foreach (var handle in handles)
                {
                    var handleRect = new Rectangle(handle.X, handle.Y, handleSize, handleSize);
                    g.FillEllipse(handleBrush, handleRect);
                    g.DrawEllipse(handlePen, handleRect);
                }
            }
        }
        
        private void DrawDimensionsText(Graphics g, Rectangle rect)
        {
            if (rect.Width > 50 && rect.Height > 30)
            {
                var dimensionText = $"{rect.Width} × {rect.Height}";
                using (var textBrush = new SolidBrush(Color.White))
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                using (var font = new Font("Segoe UI", 10, FontStyle.Regular))
                {
                    var textSize = g.MeasureString(dimensionText, font);
                    var textRect = new RectangleF(
                        rect.X + (rect.Width - textSize.Width) / 2,
                        rect.Y + (rect.Height - textSize.Height) / 2,
                        textSize.Width + 8,
                        textSize.Height + 4);
                    
                    g.FillRoundedRectangle(bgBrush, textRect, 4);
                    g.DrawString(dimensionText, font, textBrush, 
                        textRect.X + 4, textRect.Y + 2);
                }
            }
        }

        /// <summary>
        /// Calculates the selected area as a rectangle based on start and end points.
        /// </summary>
        private Rectangle GetSelectionRectangle()
        {
            return new Rectangle(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(startPoint.X - endPoint.X),
                Math.Abs(startPoint.Y - endPoint.Y)
            );
        }

        /// <summary>
        /// Closes the form when the escape key is pressed.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
    
    // Extension method for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF rect, float radius)
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseAllFigures();
                g.FillPath(brush, path);
            }
        }
    }
}
