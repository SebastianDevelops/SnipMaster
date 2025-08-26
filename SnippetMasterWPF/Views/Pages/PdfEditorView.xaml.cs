using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SnipMaster.Modification.Models;
using SnipMaster.Modification.Services;
using SnippetMasterWPF.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.Globalization;


namespace SnippetMasterWPF.Views.Pages
{
    public partial class PdfEditorView : INavigableView<PdfEditorViewModel>
    {
        public PdfEditorViewModel ViewModel { get; }
        
        private readonly Dictionary<Canvas, DrawingVisual> _canvasVisuals = new();
        private readonly Dictionary<string, GlyphTypeface> _glyphTypefaceCache = new();
        private Canvas _activeCanvas;

        public PdfEditorView(PdfEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
            
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Any property change that affects the visuals will trigger a single, unified render pass.
            if (e.PropertyName is nameof(ViewModel.CurrentLayouts) or nameof(ViewModel.CaretPosition) or nameof(ViewModel.HasSelection) or nameof(ViewModel.DocumentPages))
            {
                // Defer rendering to ensure the UI is ready.
                Dispatcher.BeginInvoke(new Action(RenderVisiblePages), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void RenderVisiblePages()
        {
            if (ViewModel.DocumentPages == null) return;

            var itemsControl = PageItemsControl;
            if (itemsControl?.ItemContainerGenerator == null) return;

            for (int i = 0; i < ViewModel.DocumentPages.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue; // Skip pages not yet generated

                container.ApplyTemplate();
                var canvas = FindChild<Canvas>(container);
                if (canvas == null) continue;

                // Get the single DrawingVisual for this canvas, or create it if it doesn't exist.
                var visual = GetOrCreateDrawingVisual(canvas);
                
                // Get the page and its layout from the ViewModel.
                var page = ViewModel.DocumentPages[i];
                var layouts = ViewModel.CurrentLayouts;

                // --- Atomic Render Pass ---
                using (DrawingContext dc = visual.RenderOpen())
                {
                    // Step 1: Draw a white background to clear the previous frame.
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, canvas.Width, canvas.Height));

                    // Step 2: Render all paragraphs' text for this page.
                    foreach (var paragraph in page.Paragraphs)
                    {
                        if (layouts != null && layouts.TryGetValue(paragraph, out var layout))
                        {
                            RenderLayoutWithGlyphRuns(dc, layout, canvas.Height); // High-performance text
                        }
                    }
                    
                    // Step 3: If this is the active page, render selection and caret on top.
                    if (ViewModel.ActiveParagraph != null && page.Paragraphs.Contains(ViewModel.ActiveParagraph))
                    {
                        RenderSelectionAndCaret(dc, ViewModel, canvas.Height);
                    }
                }
            }
        }

        private static (string FamilyName, FontWeight Weight, FontStyle Style) ParsePdfFontName(string pdfFontName)
        {
            if (string.IsNullOrEmpty(pdfFontName)) return ("Segoe UI", FontWeights.Normal, FontStyles.Normal);
            
            string workingName = pdfFontName;
            FontWeight weight = FontWeights.Normal;
            FontStyle style = FontStyles.Normal;
            
            // Strip XXXXXX+ subset prefix
            if (workingName.Length > 7 && workingName[6] == '+')
            {
                workingName = workingName.Substring(7);
            }
            
            // Parse style keywords
            var lowerName = workingName.ToLowerInvariant();
            var keywords = new[] { "bold", "italic", "oblique", "light", "black", "heavy" };
            
            foreach (var keyword in keywords)
            {
                if (lowerName.Contains(keyword))
                {
                    switch (keyword)
                    {
                        case "bold":
                            weight = FontWeights.Bold;
                            break;
                        case "italic":
                        case "oblique":
                            style = FontStyles.Italic;
                            break;
                        case "light":
                            weight = FontWeights.Light;
                            break;
                        case "black":
                        case "heavy":
                            weight = FontWeights.Black;
                            break;
                    }
                    
                    // Remove keyword and separator
                    var separators = new[] { "-" + keyword, "," + keyword, " " + keyword };
                    foreach (var sep in separators)
                    {
                        if (workingName.Contains(sep, StringComparison.OrdinalIgnoreCase))
                        {
                            workingName = workingName.Replace(sep, "", StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                }
            }
            
            return (workingName, weight, style);
        }

        private GlyphTypeface GetGlyphTypeface(string pdfFontName)
        {
            if (_glyphTypefaceCache.TryGetValue(pdfFontName, out var cached))
            {
                return cached;
            }
            
            var (family, weight, style) = ParsePdfFontName(pdfFontName);
            var typeface = new Typeface(new FontFamily(family), style, weight, FontStretches.Normal);

            if (typeface.TryGetGlyphTypeface(out var glyphTypeface))
            {
                _glyphTypefaceCache[pdfFontName] = glyphTypeface;
                return glyphTypeface;
            }
            
            // First fallback: try with normal weight and style
            SnippetMasterWPF.Services.LogService.Instance.Log($"[Renderer] FONT WARNING: Could not find '{family}' with specified style. Trying normal style.");
            var fallbackTypeface = new Typeface(new FontFamily(family), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            if (fallbackTypeface.TryGetGlyphTypeface(out var fallbackGlyphTypeface))
            {
                _glyphTypefaceCache[pdfFontName] = fallbackGlyphTypeface;
                return fallbackGlyphTypeface;
            }

            // Final fallback: Segoe UI
            SnippetMasterWPF.Services.LogService.Instance.Log($"[Renderer] FONT ERROR: Could not find '{family}'. Falling back to Segoe UI.");
            var (segoeFamily, segoeWeight, segoeStyle) = ParsePdfFontName("Segoe UI");
            var segoeTypeface = new Typeface(new FontFamily(segoeFamily), segoeStyle, segoeWeight, FontStretches.Normal);
            segoeTypeface.TryGetGlyphTypeface(out var segoeGlyphTypeface);
            _glyphTypefaceCache[pdfFontName] = segoeGlyphTypeface;
            return segoeGlyphTypeface;
        }

        private void RenderLayoutWithGlyphRuns(DrawingContext dc, IReadOnlyList<PositionedLine> layout, double pageHeight)
        {
            foreach (var line in layout)
            {
                if (!line.Glyphs.Any()) continue;

                int currentRunStart = 0;
                for (int i = 1; i <= line.Glyphs.Count; i++)
                {
                    bool isEndOfLine = (i == line.Glyphs.Count);
                    bool isStyleChange = !isEndOfLine && (
                        line.Glyphs[i].FontName != line.Glyphs[currentRunStart].FontName ||
                        line.Glyphs[i].PointSize != line.Glyphs[currentRunStart].PointSize ||
                        !line.Glyphs[i].Color.Equals(line.Glyphs[currentRunStart].Color)
                    );

                    if (isEndOfLine || isStyleChange)
                    {
                        int runLength = i - currentRunStart;
                        var runGlyphs = line.Glyphs.Skip(currentRunStart).Take(runLength).ToList();
                        var firstGlyph = runGlyphs[0];

                        var glyphTypeface = GetGlyphTypeface(firstGlyph.FontName);
                        if (glyphTypeface == null) { currentRunStart = i; continue; }

                        var brush = new SolidColorBrush(Color.FromRgb(
                            (byte)(firstGlyph.Color.R * 255),
                            (byte)(firstGlyph.Color.G * 255),
                            (byte)(firstGlyph.Color.B * 255)));
                        brush.Freeze();

                        var glyphIndices = new ushort[runLength];
                        var advanceWidths = new double[runLength];
                        var glyphOffsets = new List<Point>();

                        for (int j = 0; j < runLength; j++)
                        {
                            var glyph = runGlyphs[j];
                            glyphIndices[j] = glyphTypeface.CharacterToGlyphMap.TryGetValue(glyph.Character, out ushort glyphIndex) 
                                              ? glyphIndex 
                                              : (ushort)0;

                            advanceWidths[j] = 0;

                            var glyphOffset = new Point(
                                glyph.CalculatedBoundingBox.Left - firstGlyph.CalculatedBoundingBox.Left,
                                0
                            );
                            glyphOffsets.Add(glyphOffset);
                        }

                        var baselineOrigin = new Point(
                            firstGlyph.CalculatedBoundingBox.Left,
                            pageHeight - line.BaseLineY
                        );

                        var glyphRun = new GlyphRun(
                            glyphTypeface,
                            0, false,
                            firstGlyph.PointSize * 96.0 / 72.0,
                            glyphIndices,
                            baselineOrigin,
                            advanceWidths,
                            glyphOffsets,
                            null, null, null, null, null);
                        
                        dc.DrawGlyphRun(brush, glyphRun);
                        
                        currentRunStart = i;
                    }
                }
            }
        }
        
        private void RenderSelectionAndCaret(DrawingContext dc, PdfEditorViewModel vm, double pageHeight)
        {
            // Draw Selection
            if (vm.HasSelection && !vm.SelectionRectangle.IsEmpty)
            {
                var selectionRect = vm.SelectionRectangle;
                var transformedRect = new Rect(
                    selectionRect.X,
                    pageHeight - selectionRect.Y - selectionRect.Height,
                    selectionRect.Width,
                    selectionRect.Height);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(100, 173, 214, 255)), null, transformedRect);
            }

            // Draw Caret
            if (vm.IsCaretVisible && !vm.CaretPosition.IsEmpty)
            {
                var caretRect = vm.CaretPosition;
                var transformedRect = new Rect(
                    caretRect.X,
                    pageHeight - caretRect.Y - caretRect.Height,
                    caretRect.Width,
                    caretRect.Height);
                dc.DrawRectangle(Brushes.Black, null, transformedRect);
            }
        }

        private DrawingVisual GetOrCreateDrawingVisual(Canvas canvas)
        {
            if (_canvasVisuals.TryGetValue(canvas, out var visual))
            {
                return visual;
            }

            visual = new DrawingVisual();
            _canvasVisuals[canvas] = visual;
            
            // Add a simple host for our visual to the canvas's children.
            canvas.Children.Clear();
            canvas.Children.Add(new VisualHost(visual));
            return visual;
        }

        private void OpenPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "PDF Files (*.pdf)|*.pdf" };
            if (openFileDialog.ShowDialog() == true)
            {
                ViewModel.LoadDocument(openFileDialog.FileName);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save functionality placeholder
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;

            _activeCanvas = canvas;
            canvas.Focus(); // Ensure the canvas can receive keyboard events.
            
            // Find page index
            int pageIndex = FindPageIndex(canvas);
            
            if (pageIndex >= 0)
            {
                Point clickPoint = e.GetPosition(canvas);
                ViewModel.ActivateParagraphAtPoint(clickPoint, pageIndex, canvas.ActualHeight);
            }
        }

        private int FindPageIndex(Canvas canvas)
        {
            var itemsControl = FindParent<ItemsControl>(canvas);
            if (itemsControl?.ItemContainerGenerator == null) return -1;
            
            for (int i = 0; i < ViewModel.DocumentPages?.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (IsChildOf(canvas, container))
                {
                    return i;
                }
            }
            return -1;
        }

        private void PdfEditorView_KeyDown(object sender, KeyEventArgs e)
        {
            ViewModel.HandleKeyDown(e);
        }

        private void PdfEditorView_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && e.Text[0] >= ' ')
            {
                ViewModel.HandleTextInput(e.Text);
            }
        }

        // Helper methods for finding UI elements
        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                
                var childResult = FindChild<T>(child);
                if (childResult != null) return childResult;
            }
            return null;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            return parent is T result ? result : FindParent<T>(parent);
        }

        private static bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            if (parent == null || child == null) return false;
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }

    // A simple class to host a DrawingVisual in the logical tree.
    public class VisualHost : FrameworkElement
    {
        private readonly Visual _visual;
        public VisualHost(Visual visual) { _visual = visual; AddVisualChild(_visual); }
        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _visual;
    }
}