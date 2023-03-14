namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents an error that occurred during the composition of subgraphs.
/// </summary>
public sealed class CompositionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositionException"/> class.
    /// </summary>
    /// <param name="errors">
    /// The errors that occurred during the composition.
    /// </param>
    public CompositionException(LogEntry[] errors)
        : base("The composition of the subgraph schemas failed.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets the errors that occurred during the composition.
    /// </summary>
    /// <value>
    /// The errors that occurred during the composition.
    /// </value>
    public IReadOnlyList<LogEntry> Errors { get; }
}
