namespace HotChocolate.CostAnalysis;

/// <summary>
/// A custom <see cref="IAnalysisAlgebra{TSummary}"/> built only against the
/// public surface: the maximum selection-set depth reached, one level per
/// field, with siblings and mutually exclusive type regions both resolved
/// by their deepest branch.
/// </summary>
public sealed class MaxDepthAlgebra : IAnalysisAlgebra<int>
{
    public int Empty => 0;

    public int Field(in CollectedFieldGroup group, int child) => 1 + child;

    public int Combine(int left, int right) => Math.Max(left, right);

    public int Join(int left, int right) => Math.Max(left, right);

    public int Root(double rootTypeWeight, int selection) => selection;
}
