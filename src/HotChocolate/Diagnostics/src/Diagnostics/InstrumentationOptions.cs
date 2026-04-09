using static HotChocolate.Diagnostics.ActivityScopes;

namespace HotChocolate.Diagnostics;

/// <summary>
/// The Hot Chocolate instrumentation options.
/// </summary>
public sealed class InstrumentationOptions : InstrumentationOptionsBase
{
    /// <summary>
    /// Specifies the activity scopes that shall be instrumented.
    /// </summary>
    public ActivityScopes Scopes { get; set; } = Default;

    /// <summary>
    /// Specifies if DataLoader batch keys shall be included into the tracing data.
    /// </summary>
    public bool IncludeDataLoaderKeys { get; set; }

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
