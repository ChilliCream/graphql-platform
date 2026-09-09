namespace HotChocolate.CostAnalysis;

internal sealed class GateConfiguration
{
    public double WarmEvaluateP50Us { get; init; }

    public long WarmAllocatedBytes { get; init; }

    public double ColdRustBand { get; init; }

    public double AdversarialMs { get; init; }

    public int CaseBudget { get; init; }

    public string FrozenAt { get; init; } = string.Empty;
}

internal sealed class HeadToHeadProvenance
{
    public string GitRev { get; init; } = string.Empty;

    public string OracleGitRev { get; init; } = string.Empty;

    public int Seed { get; init; }

    public int Replicates { get; init; }

    public string Selection { get; init; } = string.Empty;
}
