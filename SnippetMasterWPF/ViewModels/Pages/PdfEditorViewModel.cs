using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SnipMaster.Modification.Models;
using SnipMaster.Modification.Services;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class PdfEditorViewModel : ObservableObject
    {
        private readonly EditorStateService _editorState;

        [ObservableProperty]
        private IReadOnlyDictionary<LiveParagraph, IReadOnlyList<PositionedLine>> _currentLayouts;
        
        [ObservableProperty]
        private Rect _caretPosition;

        [ObservableProperty]
        private Rect _selectionRectangle;

        [ObservableProperty]
        private bool _isCaretVisible;

        [ObservableProperty]
        private bool _hasSelection;

        [ObservableProperty]
        private IReadOnlyList<LivePage> _documentPages;

        [ObservableProperty]
        private LiveParagraph _activeParagraph;

        public PdfEditorViewModel(EditorStateService editorState)
        {
            _editorState = editorState;
            _editorState.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(EditorState newState)
        {
            SnippetMasterWPF.Services.LogService.Instance.Log($"[ViewModel] OnStateChanged received. New state has {newState.DocumentPages.Count} pages. Active Paragraph is {(newState.ActiveParagraph == null ? "null" : "set")}.");
            DocumentPages = newState.DocumentPages;
            CurrentLayouts = newState.PageLayouts;
            HasSelection = newState.HasSelection;
            
            // --- THIS IS THE CRITICAL ADDITION ---
            ActiveParagraph = newState.ActiveParagraph; // Expose the active paragraph to the View.
            
            if (newState.ActiveParagraph != null && newState.PageLayouts.ContainsKey(newState.ActiveParagraph))
            {
                var layout = newState.PageLayouts[newState.ActiveParagraph];
                UpdateCaretPosition(layout, newState.CaretIndex);
                UpdateSelectionRectangle(layout, newState);
                IsCaretVisible = true;
            }
            else
            {
                IsCaretVisible = false;
                HasSelection = false;
            }
        }

        private void UpdateCaretPosition(IReadOnlyList<PositionedLine> layout, int caretIndex)
        {
            int currentIndex = 0;
            
            foreach (var line in layout)
            {
                foreach (var glyph in line.Glyphs)
                {
                    if (currentIndex == caretIndex)
                    {
                        double caretHeight = glyph.PointSize * 1.2;
                        double caretTop = line.BaseLineY + (glyph.PointSize * 0.2);
                        
                        CaretPosition = new Rect(
                            glyph.CalculatedBoundingBox.Left,
                            caretTop,
                            1,
                            caretHeight);
                        return;
                    }
                    currentIndex++;
                }
            }
            
            // Edge case: caret is at the end of the paragraph
            if (layout.Any() && layout.Last().Glyphs.Any())
            {
                var lastLine = layout.Last();
                var lastGlyph = lastLine.Glyphs.Last();
                double caretHeight = lastGlyph.PointSize * 1.2;
                double caretTop = lastLine.BaseLineY + (lastGlyph.PointSize * 0.2);
                
                CaretPosition = new Rect(
                    lastGlyph.CalculatedBoundingBox.Right,
                    caretTop,
                    1,
                    caretHeight);
            }
        }

        private void UpdateSelectionRectangle(IReadOnlyList<PositionedLine> layout, EditorState state)
        {
            if (!state.HasSelection)
            {
                SelectionRectangle = Rect.Empty;
                return;
            }

            int currentIndex = 0;
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            
            foreach (var line in layout)
            {
                foreach (var glyph in line.Glyphs)
                {
                    if (currentIndex >= state.SelectionStart && currentIndex < state.SelectionStart + state.SelectionLength)
                    {
                        var bounds = glyph.CalculatedBoundingBox;
                        minX = Math.Min(minX, bounds.Left);
                        minY = Math.Min(minY, bounds.Bottom);
                        maxX = Math.Max(maxX, bounds.Right);
                        maxY = Math.Max(maxY, bounds.Top);
                    }
                    currentIndex++;
                }
            }
            
            if (minX != double.MaxValue)
            {
                SelectionRectangle = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public void LoadDocument(string filePath)
        {
            _editorState.LoadDocument(filePath);
        }
        
        public void ActivateParagraphAtPoint(System.Windows.Point viewPoint, int pageIndex, double pageHeight)
        {
            var modelPoint = new System.Drawing.Point((int)viewPoint.X, (int)(pageHeight - viewPoint.Y));
            var state = _editorState.GetCurrentState();
            
            if (pageIndex >= state.DocumentPages.Count) return;
            
            var page = state.DocumentPages[pageIndex];
            var foundParagraphIndex = -1;
            
            for (int i = 0; i < page.Paragraphs.Count; i++)
            {
                var p = page.Paragraphs[i];
                if (modelPoint.X >= p.BoundingBox.Left && modelPoint.X <= p.BoundingBox.Right &&
                    modelPoint.Y >= p.BoundingBox.Bottom && modelPoint.Y <= p.BoundingBox.Top)
                {
                    foundParagraphIndex = i;
                    break;
                }
            }
            
            if (foundParagraphIndex >= 0)
            {
                _editorState.Dispatch(new ActivateAndSetCaretAction(pageIndex, foundParagraphIndex, modelPoint));
            }
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            var state = _editorState.GetCurrentState();
            if (state.ActiveParagraph == null) return;

            bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            switch (e.Key)
            {
                case Key.Left when ctrl:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.LeftByWord, shift));
                    e.Handled = true;
                    break;
                case Key.Right when ctrl:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.RightByWord, shift));
                    e.Handled = true;
                    break;
                case Key.Left:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.Left, shift));
                    e.Handled = true;
                    break;
                case Key.Right:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.Right, shift));
                    e.Handled = true;
                    break;
                case Key.Up:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.Up, shift));
                    e.Handled = true;
                    break;
                case Key.Down:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.Down, shift));
                    e.Handled = true;
                    break;
                case Key.Home:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.Home, shift));
                    e.Handled = true;
                    break;
                case Key.End:
                    _editorState.Dispatch(new MoveCaretAction(CaretMovement.End, shift));
                    e.Handled = true;
                    break;
                case Key.A when ctrl:
                    _editorState.Dispatch(new SelectAllAction());
                    e.Handled = true;
                    break;
                case Key.X when ctrl:
                    HandleCut();
                    e.Handled = true;
                    break;
                case Key.V when ctrl:
                    HandlePaste();
                    e.Handled = true;
                    break;
                case Key.Back:
                    HandleBackspace();
                    e.Handled = true;
                    break;
                case Key.Delete:
                    HandleDelete();
                    e.Handled = true;
                    break;
            }
        }

        public void HandleTextInput(string text)
        {
            var state = _editorState.GetCurrentState();
            if (state.ActiveParagraph == null) return;
            
            _editorState.Dispatch(new InsertTextAction(state.CaretIndex, text));
        }

        public void HandleBackspace()
        {
            var state = _editorState.GetCurrentState();
            if (state.HasSelection)
            {
                _editorState.Dispatch(new DeleteAction(state.SelectionStart, state.SelectionLength));
            }
            else if (state.CaretIndex > 0)
            {
                _editorState.Dispatch(new DeleteAction(state.CaretIndex - 1, 1));
            }
        }

        public void HandleDelete()
        {
            var state = _editorState.GetCurrentState();
            if (state.HasSelection)
            {
                _editorState.Dispatch(new DeleteAction(state.SelectionStart, state.SelectionLength));
            }
            else if (state.CaretIndex < state.ActiveParagraph?.Glyphs.Count)
            {
                _editorState.Dispatch(new DeleteAction(state.CaretIndex, 1));
            }
        }

        private void HandleCut()
        {
            var state = _editorState.GetCurrentState();
            if (!state.HasSelection) return;

            string selectedText = GetTextForSelection(state);
            Clipboard.SetText(selectedText);
            _editorState.Dispatch(new CutAction());
        }

        private void HandlePaste()
        {
            string text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text)) return;
            
            _editorState.Dispatch(new PasteAction(text));
        }

        private string GetTextForSelection(EditorState state)
        {
            if (!state.HasSelection || state.ActiveParagraph == null) return string.Empty;
            
            var glyphs = state.ActiveParagraph.Glyphs;
            var selectedGlyphs = glyphs.Skip(state.SelectionStart).Take(state.SelectionLength);
            return new string(selectedGlyphs.Select(g => g.Character).ToArray());
        }
    }

    public class PageViewModel
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public ObservableCollection<UIElement> PageElements { get; } = new();
    }
}