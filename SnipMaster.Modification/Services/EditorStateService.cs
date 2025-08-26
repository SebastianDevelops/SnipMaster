using System.Diagnostics;
using SnipMaster.Modification.Models;
using System.Drawing;

namespace SnipMaster.Modification.Services;

public interface IEditorAction { }

public record LoadDocumentAction(string FilePath) : IEditorAction;
public record ActivateParagraphAction(int PageIndex, int ParagraphIndex) : IEditorAction;
public record ActivateAndSetCaretAction(int PageIndex, int ParagraphIndex, Point ClickPoint) : IEditorAction;
public record InsertTextAction(int CaretIndex, string Text) : IEditorAction;
public record DeleteAction(int CaretIndex, int Count) : IEditorAction;
public record MoveCaretAction(CaretMovement Direction, bool ShiftIsPressed) : IEditorAction;
public record SetCaretAction(int Index, bool ShiftIsPressed) : IEditorAction;
public record CutAction() : IEditorAction;
public record PasteAction(string Text) : IEditorAction;
public record SelectAllAction() : IEditorAction;

public enum CaretMovement
{
    Left, Right, LeftByWord, RightByWord, Up, Down, Home, End
}

public record EditorState(
    IReadOnlyList<LivePage> DocumentPages,
    FontMetricsCacheService MetricsCache,
    LiveParagraph ActiveParagraph,
    int SelectionAnchor,
    int CaretIndex,
    IReadOnlyDictionary<LiveParagraph, IReadOnlyList<PositionedLine>> PageLayouts
)
{
    public bool HasSelection => SelectionAnchor != CaretIndex;
    public int SelectionStart => Math.Min(SelectionAnchor, CaretIndex);
    public int SelectionLength => Math.Abs(CaretIndex - SelectionAnchor);
}

public class EditorStateService
{
    private EditorState _currentState;
    private readonly PdfParserService _parser = new();

    public event Action<EditorState> StateChanged;

    public EditorState GetCurrentState() => _currentState;

    public void LoadDocument(string filePath)
    {
        var result = _parser.ParseDocument(filePath);
        var layouts = new Dictionary<LiveParagraph, IReadOnlyList<PositionedLine>>();
        
        foreach (var page in result.Pages)
        {
            foreach (var paragraph in page.Paragraphs)
            {
                var engine = new LayoutEngine(paragraph, result.MetricsCache);
                layouts[paragraph] = engine.GetLayout();
            }
        }

        _currentState = new EditorState(
            result.Pages,
            result.MetricsCache,
            null,
            0,
            0,
            layouts
        );

        Debug.WriteLine($"[StateService] LoadDocument Complete. Initializing state with {result.Pages.Count} pages and {_currentState.PageLayouts.Count} layouts.");
        StateChanged?.Invoke(_currentState);
    }

    public void Dispatch(IEditorAction action)
    {
        var newState = Reduce(_currentState, action);
        if (newState != _currentState)
        {
            Debug.WriteLine($"[StateService] Dispatch Complete. Action: {action.GetType().Name}. Firing StateChanged event.");
            _currentState = newState;
            StateChanged?.Invoke(_currentState);
        }
    }

    private EditorState Reduce(EditorState state, IEditorAction action)
    {
        return action switch
        {
            ActivateParagraphAction activate => HandleActivateParagraph(state, activate),
            ActivateAndSetCaretAction activateAndSet => HandleActivateAndSetCaret(state, activateAndSet),
            InsertTextAction insert => HandleInsertText(state, insert),
            DeleteAction delete => HandleDelete(state, delete),
            MoveCaretAction move => HandleMoveCaret(state, move),
            SetCaretAction setCaret => HandleSetCaret(state, setCaret),
            CutAction cut => HandleCut(state, cut),
            PasteAction paste => HandlePaste(state, paste),
            SelectAllAction selectAll => HandleSelectAll(state, selectAll),
            _ => state
        };
    }

    private EditorState HandleActivateParagraph(EditorState state, ActivateParagraphAction action)
    {
        if (action.PageIndex >= state.DocumentPages.Count) return state;
        
        var page = state.DocumentPages[action.PageIndex];
        if (action.ParagraphIndex >= page.Paragraphs.Count) return state;
        
        var paragraph = page.Paragraphs[action.ParagraphIndex];
        
        return state with 
        { 
            ActiveParagraph = paragraph,
            CaretIndex = 0,
            SelectionAnchor = 0
        };
    }

    private EditorState HandleActivateAndSetCaret(EditorState state, ActivateAndSetCaretAction action)
    {
        if (action.PageIndex >= state.DocumentPages.Count) return state;
        
        var page = state.DocumentPages[action.PageIndex];
        if (action.ParagraphIndex >= page.Paragraphs.Count) return state;
        
        var paragraph = page.Paragraphs[action.ParagraphIndex];
        if (!state.PageLayouts.TryGetValue(paragraph, out var layout)) return state;
        
        // Hit-test to find the closest glyph position
        int caretIndex = HitTestForCaretIndex(layout, action.ClickPoint);
        
        return state with 
        { 
            ActiveParagraph = paragraph,
            CaretIndex = caretIndex,
            SelectionAnchor = caretIndex
        };
    }

    private int HitTestForCaretIndex(IReadOnlyList<PositionedLine> layout, Point clickPoint)
    {
        int glyphIndex = 0;
        
        foreach (var line in layout)
        {
            // Check if click is within this line's vertical bounds
            if (clickPoint.Y >= line.BaseLineY - 20 && clickPoint.Y <= line.BaseLineY + 20)
            {
                // Find closest glyph in this line
                for (int i = 0; i < line.Glyphs.Count; i++)
                {
                    var glyph = line.Glyphs[i];
                    var glyphCenter = glyph.CalculatedBoundingBox.Left + (glyph.CalculatedBoundingBox.Width / 2);
                    
                    if (clickPoint.X <= glyphCenter)
                    {
                        return glyphIndex + i;
                    }
                }
                // Click is after all glyphs in this line
                return glyphIndex + line.Glyphs.Count;
            }
            glyphIndex += line.Glyphs.Count;
        }
        
        // Click is outside all lines, place at end
        return glyphIndex;
    }

    private EditorState HandleInsertText(EditorState state, InsertTextAction action)
    {
        if (state.ActiveParagraph == null) return state;

        var stateAfterDelete = state;
        if (state.HasSelection)
        {
            stateAfterDelete = HandleDelete(state, new DeleteAction(state.SelectionStart, state.SelectionLength));
        }

        var engine = new LayoutEngine(stateAfterDelete.ActiveParagraph, stateAfterDelete.MetricsCache);
        engine.InsertText(stateAfterDelete.CaretIndex, action.Text);
        
        var newLayouts = new Dictionary<LiveParagraph, IReadOnlyList<PositionedLine>>(stateAfterDelete.PageLayouts)
        {
            [stateAfterDelete.ActiveParagraph] = engine.GetLayout()
        };

        var newCaretIndex = stateAfterDelete.CaretIndex + action.Text.Length;
        return stateAfterDelete with 
        { 
            CaretIndex = newCaretIndex,
            SelectionAnchor = newCaretIndex,
            PageLayouts = newLayouts
        };
    }

    private EditorState HandleDelete(EditorState state, DeleteAction action)
    {
        if (state.ActiveParagraph == null) return state;

        var engine = new LayoutEngine(state.ActiveParagraph, state.MetricsCache);
        engine.DeleteGlyphs(action.CaretIndex, action.Count);
        
        var newLayouts = new Dictionary<LiveParagraph, IReadOnlyList<PositionedLine>>(state.PageLayouts)
        {
            [state.ActiveParagraph] = engine.GetLayout()
        };

        var newCaretIndex = Math.Max(0, action.CaretIndex);
        return state with 
        { 
            CaretIndex = newCaretIndex,
            SelectionAnchor = newCaretIndex,
            PageLayouts = newLayouts
        };
    }

    private EditorState HandleSetCaret(EditorState state, SetCaretAction action)
    {
        int newAnchor = action.ShiftIsPressed ? state.SelectionAnchor : action.Index;
        return state with { CaretIndex = action.Index, SelectionAnchor = newAnchor };
    }

    private EditorState HandleMoveCaret(EditorState state, MoveCaretAction action)
    {
        if (state.ActiveParagraph == null) return state;
        
        int newCaretIndex = CalculateNewCaretIndex(state, action.Direction);
        int newAnchor = action.ShiftIsPressed ? state.SelectionAnchor : newCaretIndex;
        return state with { CaretIndex = newCaretIndex, SelectionAnchor = newAnchor };
    }

    private int CalculateNewCaretIndex(EditorState state, CaretMovement direction)
    {
        var glyphs = state.ActiveParagraph.Glyphs;
        var layout = state.PageLayouts[state.ActiveParagraph];
        int currentIndex = state.CaretIndex;

        switch (direction)
        {
            case CaretMovement.Left:
                return Math.Max(0, currentIndex - 1);
            case CaretMovement.Right:
                return Math.Min(glyphs.Count, currentIndex + 1);

            case CaretMovement.LeftByWord:
                return FindWordBoundary(glyphs, currentIndex, false);
            case CaretMovement.RightByWord:
                return FindWordBoundary(glyphs, currentIndex, true);

            case CaretMovement.Up:
            case CaretMovement.Down:
                return FindClosestGlyphOnAdjacentLine(layout, currentIndex, direction);

            case CaretMovement.Home:
                return FindLineBoundary(layout, currentIndex, true);
            case CaretMovement.End:
                return FindLineBoundary(layout, currentIndex, false);

            default: return currentIndex;
        }
    }

    private int FindWordBoundary(IReadOnlyList<EditableGlyph> glyphs, int currentIndex, bool forward)
    {
        if (forward)
        {
            if (currentIndex >= glyphs.Count) return glyphs.Count;
            // Scan to the end of the current word
            int i = currentIndex;
            while (i < glyphs.Count && !char.IsWhiteSpace(glyphs[i].Character))
            {
                i++;
            }
            // Scan past any subsequent whitespace
            while (i < glyphs.Count && char.IsWhiteSpace(glyphs[i].Character))
            {
                i++;
            }
            return i;
        }
        else // Backward
        {
            if (currentIndex <= 0) return 0;
            // Scan to the beginning of the current word
            int i = currentIndex - 1;
            while (i > 0 && !char.IsWhiteSpace(glyphs[i].Character))
            {
                i--;
            }
            // If we stopped on a character, the boundary is the start of that word.
            // If we stopped at the beginning, the boundary is 0.
            return char.IsWhiteSpace(glyphs[i].Character) ? i + 1 : 0;
        }
    }

    private int FindClosestGlyphOnAdjacentLine(IReadOnlyList<PositionedLine> layout, int currentIndex, CaretMovement direction)
    {
        // Find current line and X position
        int currentGlyphIndex = 0;
        int currentLineIndex = -1;
        double targetX = 0;
        
        for (int lineIdx = 0; lineIdx < layout.Count; lineIdx++)
        {
            var line = layout[lineIdx];
            if (currentGlyphIndex + line.Glyphs.Count > currentIndex)
            {
                currentLineIndex = lineIdx;
                var glyphInLine = currentIndex - currentGlyphIndex;
                if (glyphInLine < line.Glyphs.Count)
                {
                    targetX = line.Glyphs[glyphInLine].CalculatedBoundingBox.Left;
                }
                break;
            }
            currentGlyphIndex += line.Glyphs.Count;
        }
        
        if (currentLineIndex == -1) return currentIndex;
        
        int targetLineIndex = direction == CaretMovement.Up ? currentLineIndex - 1 : currentLineIndex + 1;
        if (targetLineIndex < 0 || targetLineIndex >= layout.Count) return currentIndex;
        
        // Find closest glyph on target line
        var targetLine = layout[targetLineIndex];
        int closestGlyphIndex = 0;
        double minDistance = double.MaxValue;
        
        for (int i = 0; i < targetLine.Glyphs.Count; i++)
        {
            double distance = Math.Abs(targetLine.Glyphs[i].CalculatedBoundingBox.Left - targetX);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestGlyphIndex = i;
            }
        }
        
        // Calculate global index
        int globalIndex = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            globalIndex += layout[i].Glyphs.Count;
        }
        return globalIndex + closestGlyphIndex;
    }

    // FindLineStart and FindLineEnd are replaced by this more generic helper
    private int FindLineBoundary(IReadOnlyList<PositionedLine> layout, int currentIndex, bool findStart)
    {
        int globalIndex = 0;
        foreach (var line in layout)
        {
            int lineEndIndex = globalIndex + line.Glyphs.Count;
            if (currentIndex >= globalIndex && currentIndex <= lineEndIndex)
            {
                return findStart ? globalIndex : lineEndIndex;
            }
            globalIndex = lineEndIndex;
        }
        return currentIndex; // Should not happen if caret is valid
    }

    private EditorState HandleCut(EditorState state, CutAction action)
    {
        if (!state.HasSelection) return state;
        return HandleDelete(state, new DeleteAction(state.SelectionStart, state.SelectionLength));
    }

    private EditorState HandlePaste(EditorState state, PasteAction action)
    {
        EditorState stateAfterDelete = state;
        if (state.HasSelection)
        {
            stateAfterDelete = HandleDelete(state, new DeleteAction(state.SelectionStart, state.SelectionLength));
        }
        return HandleInsertText(stateAfterDelete, new InsertTextAction(stateAfterDelete.CaretIndex, action.Text));
    }

    private EditorState HandleSelectAll(EditorState state, SelectAllAction action)
    {
        if (state.ActiveParagraph == null) return state;
        return state with 
        { 
            SelectionAnchor = 0,
            CaretIndex = state.ActiveParagraph.Glyphs.Count
        };
    }
}