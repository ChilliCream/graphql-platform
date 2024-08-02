namespace HotChocolate.Diagnostics;

[Flags]
public enum ActivityScopes
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
    CompileOperation = 256,
    ExecuteOperation = 512,
    ResolveFieldValue = 1024,
    DataLoaderBatch = 2048,
    Default =
        ExecuteHttpRequest |
        ParseHttpRequest |
        ValidateDocument |
        CompileOperation |
        ResolveFieldValue |
        FormatHttpResponse |
        DataLoaderBatch,
    All =
        ExecuteHttpRequest |
        ParseHttpRequest |
        FormatHttpResponse |
        ExecuteRequest |
        ParseDocument |
        ValidateDocument |
        AnalyzeComplexity |
        CoerceVariables |
        CompileOperation |
        ExecuteOperation |
        ResolveFieldValue |
        DataLoaderBatch,
}
