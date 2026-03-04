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

    internal override bool SkipExecuteHttpRequest => (Scopes & ExecuteHttpRequest) != ExecuteHttpRequest;

    internal override bool SkipParseHttpRequest => (Scopes & ParseHttpRequest) != ParseHttpRequest;

    internal override bool SkipFormatHttpResponse => (Scopes & FormatHttpResponse) != FormatHttpResponse;

    internal override bool SkipExecuteRequest => (Scopes & ExecuteRequest) != ExecuteRequest;

    internal override bool SkipParseDocument => (Scopes & ParseDocument) != ParseDocument;

    internal override bool SkipValidateDocument => (Scopes & ValidateDocument) != ValidateDocument;

    internal override bool SkipCoerceVariables => (Scopes & CoerceVariables) != CoerceVariables;

    internal bool SkipPlanOperation => (Scopes & PlanOperation) != PlanOperation;

    internal override bool SkipExecuteOperation => (Scopes & ExecuteOperation) != ExecuteOperation;

    internal bool SkipExecuteNodes => (Scopes & ExecuteNodes) != ExecuteNodes;
}
