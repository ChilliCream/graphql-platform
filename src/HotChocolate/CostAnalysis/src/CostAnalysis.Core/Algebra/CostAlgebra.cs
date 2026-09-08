namespace HotChocolate.CostAnalysis;

/// <summary>
/// The built-in IBM field/type cost analysis algebra.
/// </summary>
public sealed class CostAlgebra : IAnalysisAlgebra<CostEstimate>
{
    /// <inheritdoc />
    public CostEstimate Empty => throw ThrowHelper.NotImplemented();

    /// <inheritdoc />
    public CostEstimate Field(in CollectedFieldGroup group, CostEstimate child)
    {
        _ = group;
        _ = child;
        throw ThrowHelper.NotImplemented();
    }

    /// <inheritdoc />
    public CostEstimate Combine(CostEstimate left, CostEstimate right)
    {
        _ = left;
        _ = right;
        throw ThrowHelper.NotImplemented();
    }

    /// <inheritdoc />
    public CostEstimate Join(CostEstimate left, CostEstimate right)
    {
        _ = left;
        _ = right;
        throw ThrowHelper.NotImplemented();
    }
}
