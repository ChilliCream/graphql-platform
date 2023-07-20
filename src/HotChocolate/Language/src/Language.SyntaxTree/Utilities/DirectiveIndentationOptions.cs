namespace HotChocolate.Language.Utilities;

/// <summary>
/// Defines the indentation options for directives.
/// </summary>
public sealed class DirectiveIndentationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectiveIndentationOptions" /> class.
    /// </summary>
    /// <param name="newLineCount"></param>
    public DirectiveIndentationOptions(int newLineCount)
    {
        NewLineCount = newLineCount;
    }

    /// <summary>
    /// Write directive on separate line if a type system
    /// member has more than this number of directives.
    /// </summary>
    public int NewLineCount { get; }
}