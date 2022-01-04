namespace HotChocolate.Diagnostics;

public sealed class InstrumentationOptions
{
    public bool IncludeRequest { get; set; }

    public bool IncludeDocument { get; set; }

    public bool SkipParseDocument { get; set; }

    public bool SkipValidateDocument { get; set; }

    public bool SkipAnalyzeOperationComplexity { get; set; }

    public bool SkipCoerceVariables { get; set; }

    public bool SkipCompileOperation { get; set; }

    public bool SkipBuildQueryPlan { get; set; }

    public bool SkipExecuteOperation { get; set; }
}
