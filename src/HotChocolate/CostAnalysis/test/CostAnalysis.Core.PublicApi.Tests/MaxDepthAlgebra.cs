namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes the maximum selection depth, counting one level per field.
/// </summary>
public sealed class MaxDepthAlgebra : IAnalysisAlgebra<int>
{
    public int Empty => 0;

    public int Field(in CollectedFieldGroup group, int child) => 1 + child;

    public int Combine(int left, int right) => Math.Max(left, right);

    public int Join(int left, int right) => Math.Max(left, right);

    public int Root(double rootTypeWeight, int selection) => selection;
}
