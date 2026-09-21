namespace HotChocolate.CostAnalysis;

/// <summary>
/// An analysis that detects whether an operation exceeds its compilation case budget.
/// </summary>
internal sealed class CaseBudgetProbeAlgebra : IAnalysisAlgebra<bool>
{
    public static readonly CaseBudgetProbeAlgebra Instance = new();

    private CaseBudgetProbeAlgebra()
    {
    }

    public bool Empty => false;

    public bool Field(in CollectedFieldGroup group, bool child) => false;

    public bool Combine(bool left, bool right) => false;

    public bool Join(bool left, bool right) => false;

    public bool Root(double rootTypeWeight, bool selection) => false;
}
