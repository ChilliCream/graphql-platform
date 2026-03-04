using HotChocolate.Diagnostics;
using static HotChocolate.Fusion.Diagnostics.FusionActivityScopes;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// The Hot Chocolate Fusion instrumentation options.
/// </summary>
public sealed class InstrumentationOptions : InstrumentationOptionsBase
{
    /// <summary>
    /// Specifies the activity scopes that shall be instrumented.
    /// </summary>
    public FusionActivityScopes Scopes { get; set; } = Default;

    internal bool SkipExecuteHttpRequest => (Scopes & ExecuteHttpRequest) != ExecuteHttpRequest;

    internal bool SkipParseHttpRequest => (Scopes & ParseHttpRequest) != ParseHttpRequest;

    internal bool SkipFormatHttpResponse => (Scopes & FormatHttpResponse) != FormatHttpResponse;

    internal bool SkipExecuteRequest => (Scopes & ExecuteRequest) != ExecuteRequest;

    internal bool SkipParseDocument => (Scopes & ParseDocument) != ParseDocument;

    internal bool SkipValidateDocument => (Scopes & ValidateDocument) != ValidateDocument;

    internal bool SkipAnalyzeComplexity => (Scopes & AnalyzeComplexity) != AnalyzeComplexity;

    internal bool SkipCoerceVariables => (Scopes & CoerceVariables) != CoerceVariables;

    internal bool SkipPlanOperation => (Scopes & PlanOperation) != PlanOperation;

    internal bool SkipExecuteOperation => (Scopes & ExecuteOperation) != ExecuteOperation;

    internal bool SkipExecutePlanNodes => (Scopes & ExecutePlanNodes) != ExecutePlanNodes;
}
