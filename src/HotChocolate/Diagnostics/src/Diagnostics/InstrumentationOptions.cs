namespace HotChocolate.Diagnostics;

public sealed class InstrumentationOptions
{
    public bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;

    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    public bool IncludeDocument { get; set; }

    public bool SkipParseRequest { get; set; }

    public bool SkipParseDocument { get; set; }

    public bool SkipValidateDocument { get; set; }

    public bool SkipAnalyzeOperationComplexity { get; set; }

    public bool SkipCoerceVariables { get; set; }

    public bool SkipCompileOperation { get; set; }

    public bool SkipBuildQueryPlan { get; set; }

    public bool SkipExecuteOperation { get; set; }
}
