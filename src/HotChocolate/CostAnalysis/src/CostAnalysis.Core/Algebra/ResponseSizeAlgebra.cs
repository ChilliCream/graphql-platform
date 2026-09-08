namespace HotChocolate.CostAnalysis;

/// <summary>
/// The built-in maximum response size analysis algebra.
/// </summary>
public sealed class ResponseSizeAlgebra : IAnalysisAlgebra<double>
{
    /// <inheritdoc />
    public double Empty => throw ThrowHelper.NotImplemented();

    /// <inheritdoc />
    public double Field(in CollectedFieldGroup group, double child)
    {
        _ = group;
        _ = child;
        throw ThrowHelper.NotImplemented();
    }

    /// <inheritdoc />
    public double Combine(double left, double right)
    {
        _ = left;
        _ = right;
        throw ThrowHelper.NotImplemented();
    }

    /// <inheritdoc />
    public double Join(double left, double right)
    {
        _ = left;
        _ = right;
        throw ThrowHelper.NotImplemented();
    }
}
