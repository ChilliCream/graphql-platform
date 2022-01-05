namespace HotChocolate.Diagnostics;

/// <summary>
/// The Hot Chocolate instrumentation options.
/// </summary>
public sealed class InstrumentationOptions
{
    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;

    /// <summary>
    /// Specifies the request detail that shall be included into the tracing activities.
    /// </summary>
    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    /// <summary>
    /// Specifies if the parsed document shall be included into the tracing data.
    /// </summary>
    public bool IncludeDocument { get; set; }

    /// <summary>
    /// Specifies if the request parser shall be instrumented.
    /// </summary>
    public bool SkipParseRequest { get; set; }

    /// <summary>
    /// Specifies if the document parser shall be instrumented.
    /// </summary>
    public bool SkipParseDocument { get; set; }

    /// <summary>
    /// Specifies if the validation shall be instrumented.
    /// </summary>
    public bool SkipValidateDocument { get; set; }

    /// <summary>
    /// Specifies if the complexity analysis shall be instrumented.
    /// </summary>
    public bool SkipAnalyzeOperationComplexity { get; set; }

    /// <summary>
    /// Specifies if the variable coercion shall be instrumented.
    /// </summary>
    public bool SkipCoerceVariables { get; set; }

    /// <summary>
    /// Specifies if the operation compilation shall be instrumented.
    /// </summary>
    public bool SkipCompileOperation { get; set; }

    /// <summary>
    /// Specifies if the operation execution shall be instrumented.
    /// </summary>
    public bool SkipExecuteOperation { get; set; }

    /// <summary>
    /// Specifies if the resolvers shall be instrumented.
    /// </summary>
    public bool SkipResolveFieldValue { get; set; }
}
