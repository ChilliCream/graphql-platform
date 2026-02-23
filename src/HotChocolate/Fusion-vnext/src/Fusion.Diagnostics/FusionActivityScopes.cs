namespace HotChocolate.Fusion.Diagnostics;

[Flags]
public enum FusionActivityScopes
{
    None = 0,
    ExecuteHttpRequest = 1,
    ParseHttpRequest = 2,
    FormatHttpResponse = 4,
    ExecuteRequest = 8,
    ParseDocument = 16,
    ValidateDocument = 32,
    AnalyzeComplexity = 64,
    CoerceVariables = 128,
    PlanOperation = 256,
    ExecuteOperation = 512,
    Default =
        ExecuteHttpRequest
        | ParseHttpRequest
        | ValidateDocument
        | PlanOperation
        | FormatHttpResponse,
    All =
        ExecuteHttpRequest
        | ParseHttpRequest
        | FormatHttpResponse
        | ExecuteRequest
        | ParseDocument
        | ValidateDocument
        | AnalyzeComplexity
        | CoerceVariables
        | PlanOperation
        | ExecuteOperation
}
