namespace HotChocolate.Language;

/// <summary>
/// The GraphQL parser options.
/// </summary>
public sealed class ParserOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="ParserOptions"/>.
    /// </summary>
    /// <param name="noLocations">
    /// Defines that the parse shall not preserve syntax node locations.
    /// </param>
    /// <param name="allowFragmentVariables">
    /// Defines that the parser shall parse fragment variables.
    /// </param>
    /// <param name="maxAllowedNodes">
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of nodes allowed within a document.
    /// </param>
    /// <param name="maxAllowedTokens">
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of tokens allowed within a document.
    /// </param>
    /// <param name="maxAllowedFields">
    /// The maximum number of fields allowed within a query document.
    /// </param>
    /// <param name="maxAllowedDirectives">
    /// The maximum number of directives allowed per location (e.g. per field, per operation).
    /// </param>
    /// <param name="maxAllowedRecursionDepth">
    /// The maximum allowed recursion depth when parsing a document.
    /// This prevents stack overflow from deeply nested queries.
    /// </param>
    public ParserOptions(
        bool noLocations = false,
        bool allowFragmentVariables = false,
        int maxAllowedNodes = int.MaxValue,
        int maxAllowedTokens = int.MaxValue,
        int maxAllowedFields = 2048,
        int maxAllowedDirectives = 4,
        int maxAllowedRecursionDepth = 200)
    {
        NoLocations = noLocations;
        Experimental = new(allowFragmentVariables);
        MaxAllowedTokens = maxAllowedTokens;
        MaxAllowedNodes = maxAllowedNodes;
        MaxAllowedFields = maxAllowedFields;
        MaxAllowedDirectives = maxAllowedDirectives;
        MaxAllowedRecursionDepth = maxAllowedRecursionDepth;
    }

    /// <summary>
    /// By default, the parser creates <see cref="ISyntaxNode" />s
    /// that know the location in the source that they correspond to.
    /// This configuration flag disables that behavior
    /// for performance or testing.
    /// </summary>
    public bool NoLocations { get; }

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of tokens allowed within a document.
    /// </summary>
    public int MaxAllowedTokens { get; }

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of nodes allowed within a document.
    /// </summary>
    public int MaxAllowedNodes { get; }

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of fields allowed within a document
    /// as fields is an easier way to estimate query size for GraphQL requests.
    /// </summary>
    public int MaxAllowedFields { get; }

    /// <summary>
    /// The maximum number of directives allowed per location (e.g. per field,
    /// per operation, per fragment definition). Repeatable directives can be used
    /// to exhaust CPU and memory resources if not limited.
    /// </summary>
    public int MaxAllowedDirectives { get; }

    /// <summary>
    /// Gets the maximum allowed recursion depth of a parsed document.
    /// </summary>
    public int MaxAllowedRecursionDepth { get; }

    /// <summary>
    /// Gets the experimental parser options
    /// which are by default switched of.
    /// </summary>
    public ParserOptionsExperimental Experimental { get; }

    /// <summary>
    /// Gets the default parser options.
    /// </summary>
    public static ParserOptions Default { get; } = new();

    /// <summary>
    /// Gets the default parser options with the locations switched of.
    /// </summary>
    public static ParserOptions NoLocation { get; } = new(noLocations: true);
}
