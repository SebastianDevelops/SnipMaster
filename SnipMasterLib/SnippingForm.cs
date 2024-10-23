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
            this.BackColor = Color.White;
            this.Opacity = 0.5;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
        }

        private Point startPoint;
        private Point endPoint;
        private bool isDragging;
        private Rectangle selectionRectangle;

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
        /// Paints the selection rectangle on the form.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isDragging)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, GetSelectionRectangle());
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
}
