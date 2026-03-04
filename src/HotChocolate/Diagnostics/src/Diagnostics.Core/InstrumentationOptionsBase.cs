namespace HotChocolate.Diagnostics;

/// <summary>
/// Base class for Hot Chocolate instrumentation options.
/// </summary>
public abstract class InstrumentationOptionsBase
{
    /// <summary>
    /// Specifies the request detail that shall be included into the tracing activities.
    /// </summary>
    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    /// <summary>
    /// Specifies if the parsed document shall be included into the tracing data.
    /// </summary>
    public bool IncludeDocument { get; set; }

    /// <summary>
    /// Defines if the operation display name shall be included in the root activity.
    /// </summary>
    public bool RenameRootActivity { get; set; }

    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;

    internal abstract bool SkipExecuteHttpRequest { get; }

    internal abstract bool SkipParseHttpRequest { get; }

    internal abstract bool SkipFormatHttpResponse { get; }

    internal abstract bool SkipExecuteRequest { get; }

    internal abstract bool SkipParseDocument { get; }

    internal abstract bool SkipValidateDocument { get; }

    internal abstract bool SkipCoerceVariables { get; }

    internal abstract bool SkipExecuteOperation { get; }
}
