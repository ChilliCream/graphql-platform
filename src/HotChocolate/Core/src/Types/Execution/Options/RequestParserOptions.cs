using HotChocolate.Language;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the request parser options.
/// </summary>
public sealed class RequestParserOptions
{
    /// <summary>
    /// Specifies if locations shall be preserved in syntax nodes so that errors can
    /// later refer to locations of the original source text.
    /// These location objects will take up extra memory.
    ///
    /// Default: <c>true</c>
    /// </summary>
    public bool IncludeLocations { get; set; } = true;

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of nodes allowed within a document.
    ///
    /// This limitation effects the <see cref="Utf8GraphQLParser"/>.
    /// </summary>
    public int MaxAllowedNodes { get; set; } = int.MaxValue;

    /// <summary>
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    ///
    /// To prevent this you can set a maximum number of tokens allowed within a document.
    ///
    /// This limitation effects the <see cref="Utf8GraphQLReader"/>.
    /// </summary>
    public int MaxAllowedTokens { get; set; } = int.MaxValue;

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
