namespace HotChocolate.Fusion.Execution.CostAnalysis;

[Flags]
internal enum CostAnalysisMode
{
    Skip = 0,
    Analyze = 1,
    Report = 2,
    Enforce = 4,
    Execute = 8
}
