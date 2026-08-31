using System.Buffers;
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
        => Connect(x, y, directions, style, owner, dashed, 0, 0);

    internal void Connect(
        int x,
        int y,
        CanvasDirections directions,
        Style style,
        object owner,
        bool dashed,
        int rank,
        int ordinal)
    {
        if (!Contains(x, y))
        {
            return;
        }

        ref var cell = ref GetState(x, y);
        cell.Directions |= directions;
        AddOwner(ref cell, owner);
        ApplyContribution(ref cell, style, dashed, rank, ordinal);
    }

    internal void SetArrow(
        int x,
        int y,
        char glyph,
        Style style,
        object owner,
        bool dashed,
        int rank,
        int ordinal)
    {
        if (!Contains(x, y))
        {
            return;
        }

        ref var cell = ref GetState(x, y);
        AddOwner(ref cell, owner);
        var canReplaceStrokeGlyph = !cell.HasStyle
            || IsAtLeastPriority(rank, ordinal, cell.Rank, cell.Ordinal);
        ApplyContribution(ref cell, style, dashed, rank, ordinal);
        if (canReplaceStrokeGlyph
            && (!cell.HasExplicitGlyph || IsAtLeastPriority(rank, ordinal, cell.GlyphRank, cell.GlyphOrdinal)))
        {
            cell.Glyph = glyph;
            cell.HasExplicitGlyph = true;
            cell.GlyphRank = rank;
            cell.GlyphOrdinal = ordinal;
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
        return new CanvasCell(
            glyph,
            cell.HasStyle ? cell.Style : Style.Plain,
            new CellOwners(cell.Owner, cell.AdditionalOwners));
    }

    /// <summary>
    /// Creates a terminal renderable for the requested rectangular window.
    /// </summary>
    public IRenderable Render(CanvasViewport viewport) => new BufferRenderable(this, viewport);

    internal IEnumerable<Segment> GetSegments(CanvasViewport viewport, int maxWidth)
    {
        var width = Math.Max(0, Math.Min(viewport.Width, maxWidth));
        var height = Math.Max(0, viewport.Height);
        var characters = ArrayPool<char>.Shared.Rent(Math.Max(1, width));
        try
        {
            for (var row = 0; row < height; row++)
            {
                var length = 0;
                var style = Style.Plain;
                var hasStyle = false;
                for (var column = 0; column < width; column++)
                {
                    var cell = Get(viewport.X + column, viewport.Y + row);
                    if (hasStyle && cell.Style != style)
                    {
                        yield return new Segment(new string(characters, 0, length), style);
                        length = 0;
                    }

                    style = cell.Style;
                    hasStyle = true;
                    characters[length++] = cell.Glyph;
                }

                if (length > 0)
                {
                    yield return new Segment(new string(characters, 0, length), style);
                }

                if (row < height - 1)
                {
                    yield return Segment.LineBreak;
                }
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(characters);
        }
    }

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

    private static void AddOwner(ref CellState cell, object owner)
    {
        if (cell.Owner is null)
        {
            cell.Owner = owner;
            return;
        }

        if (ReferenceEquals(cell.Owner, owner) || cell.Owner.Equals(owner))
        {
            return;
        }

        var additional = cell.AdditionalOwners;
        if (additional is null)
        {
            cell.AdditionalOwners = [owner];
            return;
        }

        if (!additional.Contains(owner))
        {
            additional.Add(owner);
        }
    }

    private static void ApplyContribution(
        ref CellState cell,
        Style style,
        bool dashed,
        int rank,
        int ordinal)
    {
        if (!cell.HasStyle || IsHigherPriority(rank, ordinal, cell.Rank, cell.Ordinal))
        {
            cell.Style = style;
            cell.Dashed = dashed;
            cell.HasStyle = true;
            cell.Rank = rank;
            cell.Ordinal = ordinal;
        }
    }

    private static bool IsHigherPriority(int rank, int ordinal, int otherRank, int otherOrdinal)
        => rank > otherRank || (rank == otherRank && ordinal < otherOrdinal);

    private static bool IsAtLeastPriority(int rank, int ordinal, int otherRank, int otherOrdinal)
        => rank > otherRank || (rank == otherRank && ordinal <= otherOrdinal);

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
            => buffer.GetSegments(viewport, maxWidth);
    }

    private struct CellState
    {
        public object? Owner;
        public List<object>? AdditionalOwners;
        public char Glyph;
        public Style Style;
        public CanvasDirections Directions;
        public int Rank;
        public int Ordinal;
        public int GlyphRank;
        public int GlyphOrdinal;
        public bool Dashed;
        public bool HasStyle;
        public bool HasExplicitGlyph;
    }
}
