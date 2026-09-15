namespace HotChocolate.CostAnalysis;

/// <summary>
/// A minimal algebra used only to determine whether compiling an
/// <see cref="AnalysisPlan"/> exhausts the case budget. Case-budget
/// consumption depends only on the operation's condition-tree shape and the
/// schema's case budget, never on the summary values an algebra computes, so
/// one throwaway traversal with this algebra determines the outcome for
/// every algebra the compiled plan is later evaluated with.
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
