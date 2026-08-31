using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// A two-dimensional buffer of styled terminal cells.
/// </summary>
internal sealed class CellBuffer
{
    private readonly CellState[] _cells;

    public CellBuffer(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        _cells = new CellState[Width * Height];
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>
    /// Sets a cell without changing its edge ownership metadata.
    /// </summary>
    public void Set(int x, int y, char glyph, Style? style = null)
    {
        if (!Contains(x, y))
        {
            return;
        }

        ref var cell = ref GetState(x, y);
        cell.Glyph = glyph;
        cell.Style = style ?? Style.Plain;
        cell.HasStyle = true;
        cell.HasExplicitGlyph = true;
    }

    /// <summary>
    /// Adds line directions to a cell and associates the cell with an owner.
    /// </summary>
    public void Connect(
        int x,
        int y,
        CanvasDirections directions,
        Style style,
        object owner,
        bool dashed = false)
    {
        if (!Contains(x, y))
        {
            return;
        }

        ref var cell = ref GetState(x, y);
        cell.Directions |= directions;
        cell.Style = style;
        cell.HasStyle = true;
        cell.Dashed |= dashed;
        cell.Owners ??= [];
        if (!cell.Owners.Contains(owner))
        {
            cell.Owners.Add(owner);
        }
    }

    /// <summary>
    /// Gets a cell, returning an empty cell outside the buffer bounds.
    /// </summary>
    public CanvasCell Get(int x, int y)
    {
        if (!Contains(x, y))
        {
            return CanvasCell.Empty;
        }

        ref var cell = ref GetState(x, y);
        var glyph = cell.HasExplicitGlyph ? cell.Glyph : GlyphFor(cell.Directions, cell.Dashed);
        return new CanvasCell(glyph, cell.HasStyle ? cell.Style : Style.Plain, cell.Owners ?? []);
    }

    /// <summary>
    /// Creates a terminal renderable for the requested rectangular window.
    /// </summary>
    public IRenderable Render(CanvasViewport viewport) => new BufferRenderable(this, viewport);

    /// <summary>
    /// Returns the plain text contained in a rectangular window.
    /// </summary>
    public string ToText(CanvasViewport viewport)
    {
        var lines = new string[Math.Max(0, viewport.Height)];
        for (var row = 0; row < lines.Length; row++)
        {
            var characters = new char[Math.Max(0, viewport.Width)];
            for (var column = 0; column < characters.Length; column++)
            {
                characters[column] = Get(viewport.X + column, viewport.Y + row).Glyph;
            }

            lines[row] = new string(characters);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    private ref CellState GetState(int x, int y) => ref _cells[(y * Width) + x];

    private static char GlyphFor(CanvasDirections directions, bool dashed)
    {
        if (directions == CanvasDirections.None)
        {
            return ' ';
        }

        if (dashed && (directions == (CanvasDirections.Left | CanvasDirections.Right)))
        {
            return '┄';
        }

        if (dashed && (directions == (CanvasDirections.Up | CanvasDirections.Down)))
        {
            return '┆';
        }

        return directions switch
        {
            CanvasDirections.Left => '─',
            CanvasDirections.Right => '─',
            CanvasDirections.Left | CanvasDirections.Right => '─',
            CanvasDirections.Up => '│',
            CanvasDirections.Down => '│',
            CanvasDirections.Up | CanvasDirections.Down => '│',
            CanvasDirections.Right | CanvasDirections.Down => '┌',
            CanvasDirections.Left | CanvasDirections.Down => '┐',
            CanvasDirections.Right | CanvasDirections.Up => '└',
            CanvasDirections.Left | CanvasDirections.Up => '┘',
            CanvasDirections.Up | CanvasDirections.Right | CanvasDirections.Down => '├',
            CanvasDirections.Up | CanvasDirections.Left | CanvasDirections.Down => '┤',
            CanvasDirections.Left | CanvasDirections.Right | CanvasDirections.Up => '┴',
            CanvasDirections.Left | CanvasDirections.Right | CanvasDirections.Down => '┬',
            CanvasDirections.Up | CanvasDirections.Right | CanvasDirections.Down | CanvasDirections.Left => '┼',
            _ => '┼'
        };
    }

    private sealed class BufferRenderable(CellBuffer buffer, CanvasViewport viewport) : Renderable
    {
        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var width = Math.Max(0, viewport.Width);
            var height = Math.Max(0, viewport.Height);
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    var cell = buffer.Get(viewport.X + column, viewport.Y + row);
                    yield return new Segment(cell.Glyph.ToString(), cell.Style);
                }

                if (row < height - 1)
                {
                    yield return Segment.LineBreak;
                }
            }
        }
    }

    private struct CellState
    {
        public char Glyph;
        public Style Style;
        public CanvasDirections Directions;
        public List<object>? Owners;
        public bool Dashed;
        public bool HasStyle;
        public bool HasExplicitGlyph;
    }
}
