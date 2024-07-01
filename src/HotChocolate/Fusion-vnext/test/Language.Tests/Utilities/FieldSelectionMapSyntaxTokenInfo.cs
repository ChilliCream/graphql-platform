namespace HotChocolate.Fusion;

internal sealed class FieldSelectionMapSyntaxTokenInfo(
    FieldSelectionMapTokenKind kind,
    int start,
    int end,
    int line,
    int column)
{
    /// <summary>
    /// Gets the kind of token.
    /// </summary>
    public FieldSelectionMapTokenKind Kind { get; } = kind;

    /// <summary>
    /// Gets the character offset at which this token begins.
    /// </summary>
    public int Start { get; } = start;

    /// <summary>
    /// Gets the character offset at which this token ends.
    /// </summary>
    public int End { get; } = end;

    /// <summary>
    /// Gets the 1-indexed line number on which this token appears.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-indexed column number at which this token begins.
    /// </summary>
    public int Column { get; } = column;

    public static FieldSelectionMapSyntaxTokenInfo FromReader(FieldSelectionMapReader reader)
        => new(reader.TokenKind, reader.Start, reader.End, reader.Line, reader.Column);
}
