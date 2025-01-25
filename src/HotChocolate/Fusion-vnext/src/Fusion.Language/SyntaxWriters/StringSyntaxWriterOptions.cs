namespace HotChocolate.Fusion;

/// <summary>
/// Options used when writing syntax.
/// </summary>
internal sealed class StringSyntaxWriterOptions
{
    /// <summary>
    /// Gets or sets the number of spaces to use for indentation.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the newline string used when writing syntax.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;
}
