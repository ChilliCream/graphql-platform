namespace HotChocolate.Fusion.Language;

internal readonly ref struct TokenInfo(int start, int end, int line, int column)
{
    /// <summary>
    /// Gets the character offset at which this node begins.
    /// </summary>
    public int Start { get; } = start;

    /// <summary>
    /// Gets the character offset at which this node ends.
    /// </summary>
    public int End { get; } = end;

    /// <summary>
    /// Gets the 1-indexed line number on which this node appears.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-indexed column number at which this node begins.
    /// </summary>
    public int Column { get; } = column;
}
