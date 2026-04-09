using HotChocolate.Language;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the request parser options.
/// </summary>
public sealed class RequestParserOptions
{
    /// <summary>
    /// <para>
    /// Specifies if locations shall be preserved in syntax nodes so that errors can
    /// later refer to locations of the original source text.
    /// These location objects will take up extra memory.
    /// </para>
    /// <para>Default: <c>true</c></para>
    /// </summary>
    public bool IncludeLocations { get; set; } = true;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of nodes in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    /// </para>
    /// <para>To prevent this you can set a maximum number of nodes allowed within a document.</para>
    /// <para>This limitation effects the <see cref="Utf8GraphQLParser"/>.</para>
    /// </summary>
    public int MaxAllowedNodes { get; set; } = int.MaxValue;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of tokens in a document
    /// however in extreme cases it becomes quadratic due to memory exhaustion.
    /// Parsing happens before validation so even invalid queries can burn lots of
    /// CPU time and memory.
    /// </para>
    /// <para>To prevent this you can set a maximum number of tokens allowed within a document.</para>
    /// <para>This limitation effects the <see cref="Utf8GraphQLReader"/>.</para>
    /// </summary>
    public int MaxAllowedTokens { get; set; } = int.MaxValue;

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
    /// The maximum allowed recursion depth when parsing a document.
    /// This prevents stack overflow from deeply nested queries.
    /// </para>
    /// <para>Default: <c>200</c></para>
    /// </summary>
    public int MaxAllowedRecursionDepth { get; set; } = 200;
}
