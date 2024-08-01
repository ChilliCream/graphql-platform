using static HotChocolate.Diagnostics.ActivityScopes;

namespace HotChocolate.Diagnostics;

/// <summary>
/// The Hot Chocolate instrumentation options.
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>
    /// Specifies the request detail that shall be included into the tracing activities.
    /// </summary>
    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    /// <summary>
    /// Specifies the activity scopes that shall be instrumented.
    /// </summary>
    public ActivityScopes Scopes { get; set; } = ActivityScopes.Default;

    /// <summary>
    /// Specifies if the parsed document shall be included into the tracing data.
    /// </summary>
    public bool IncludeDocument { get; set; }

    /// <summary>
    /// Specifies if DataLoader batch keys shall included into the tracing data.
    /// </summary>
    public bool IncludeDataLoaderKeys { get; set; }

    /// <summary>
    /// Defines if the operation display name shall be included in the root activity.
    /// </summary>
    public bool RenameRootActivity { get; set; }

    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;

    internal bool SkipExecuteHttpRequest => (Scopes & ExecuteHttpRequest) != ExecuteHttpRequest;

    internal bool SkipParseHttpRequest => (Scopes & ParseHttpRequest) != ParseHttpRequest;

    internal bool SkipFormatHttpResponse => (Scopes & FormatHttpResponse) != FormatHttpResponse;

    internal bool SkipExecuteRequest => (Scopes & ExecuteRequest) != ExecuteRequest;

    internal bool SkipParseDocument => (Scopes & ParseDocument) != ParseDocument;

    internal bool SkipValidateDocument => (Scopes & ValidateDocument) != ValidateDocument;

    internal bool SkipAnalyzeComplexity => (Scopes & AnalyzeComplexity) != AnalyzeComplexity;

    internal bool SkipCoerceVariables => (Scopes & CoerceVariables) != CoerceVariables;

    internal bool SkipCompileOperation => (Scopes & CompileOperation) != CompileOperation;

    internal bool SkipExecuteOperation => (Scopes & ExecuteOperation) != ExecuteOperation;

    internal bool SkipResolveFieldValue => (Scopes & ResolveFieldValue) != ResolveFieldValue;

    internal bool SkipDataLoaderBatch => (Scopes & DataLoaderBatch) != DataLoaderBatch;
}
