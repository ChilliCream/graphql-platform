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

    internal override bool SkipExecuteHttpRequest => (Scopes & ExecuteHttpRequest) != ExecuteHttpRequest;

    internal override bool SkipParseHttpRequest => (Scopes & ParseHttpRequest) != ParseHttpRequest;

    internal override bool SkipFormatHttpResponse => (Scopes & FormatHttpResponse) != FormatHttpResponse;

    internal override bool SkipExecuteRequest => (Scopes & ExecuteRequest) != ExecuteRequest;

    internal override bool SkipParseDocument => (Scopes & ParseDocument) != ParseDocument;

    internal override bool SkipValidateDocument => (Scopes & ValidateDocument) != ValidateDocument;

    internal bool SkipAnalyzeComplexity => (Scopes & AnalyzeComplexity) != AnalyzeComplexity;

    internal override bool SkipCoerceVariables => (Scopes & CoerceVariables) != CoerceVariables;

    internal bool SkipCompileOperation => (Scopes & CompileOperation) != CompileOperation;

    internal override bool SkipExecuteOperation => (Scopes & ExecuteOperation) != ExecuteOperation;

    internal bool SkipResolveFieldValue => (Scopes & ResolveFieldValue) != ResolveFieldValue;

    internal bool SkipDataLoaderBatch => (Scopes & DataLoaderBatch) != DataLoaderBatch;
}
