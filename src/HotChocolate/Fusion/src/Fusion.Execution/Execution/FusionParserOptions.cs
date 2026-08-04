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
    /// <para>
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    /// </para>
    /// <para>To prevent this you can set a maximum number of tokens allowed within a document.</para>
    /// </summary>
    public int MaxAllowedTokens { get; set; } = int.MaxValue;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    /// </para>
    /// <para>To prevent this you can set a maximum number of nodes allowed within a document.</para>
    /// </summary>
    public int MaxAllowedNodes { get; set; } = int.MaxValue;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    /// </para>
    /// <para>
    /// To prevent this you can set a maximum number of fields allowed within a document
    /// as fields is an easier way to estimate query size for GraphQL requests.
    /// </para>
    /// </summary>
    public int MaxAllowedFields { get; set; } = 2048;

    /// <summary>
    /// <para>
    /// The maximum number of directives allowed per location (e.g. per field,
    /// per operation, per fragment definition). Repeatable directives can be used
    /// to exhaust CPU and memory resources if not limited.
    /// </para>
    /// </summary>
    public int MaxAllowedDirectives { get; set; } = 4;

    /// <summary>
    /// <para>
    /// The maximum allowed recursion depth when parsing a document.
    /// This prevents stack overflow from deeply nested queries.
    /// </para>
    /// </summary>
    public int MaxAllowedRecursionDepth { get; set; } = 200;
}
