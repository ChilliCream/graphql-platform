namespace HotChocolate.CostAnalysis;

/// <summary>
/// Counts fields and their children, taking the maximum across mutually exclusive alternatives.
/// </summary>
public sealed class FieldCountAlgebra : IAnalysisAlgebra<int>
{
    public int Empty => 0;

    public int Field(in CollectedFieldGroup group, int child) => 1 + child;

    public int Combine(int left, int right) => left + right;

    public int Join(int left, int right) => Math.Max(left, right);

    public int Root(double rootTypeWeight, int selection) => selection;
}
