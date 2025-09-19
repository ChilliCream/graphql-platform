using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The GraphQL parser options.
/// </summary>
public sealed class FusionParserOptions
{
    /// <summary>
    /// By default, the parser creates <see cref="ISyntaxNode" />s
    /// that know the location in the source that they correspond to.
    /// This configuration flag disables that behavior
    /// for performance or testing.
    /// </summary>
    public bool NoLocations { get; set; }

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of tokens allowed within a document.
    /// </summary>
    public int MaxAllowedTokens { get; set; } = int.MaxValue;

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of nodes allowed within a document.
    /// </summary>
    public int MaxAllowedNodes { get; set; } = int.MaxValue;

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of fields allowed within a document
    /// as fields is an easier way to estimate query size for GraphQL requests.
    /// </summary>
    public int MaxAllowedFields { get; set; } = 2048;
}
