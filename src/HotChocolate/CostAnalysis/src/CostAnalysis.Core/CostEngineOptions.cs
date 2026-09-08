namespace HotChocolate.CostAnalysis;

/// <summary>
/// Configures the cost engine's schema-level behavior.
/// </summary>
public sealed class CostEngineOptions
{
    /// <summary>
    /// Gets or sets the list size used for a list-typed field that carries no
    /// <c>@listSize</c> annotation and no slicing arguments. The default is
    /// <see cref="double.PositiveInfinity"/>.
    /// </summary>
    public double DefaultListSize { get; set; } = double.PositiveInfinity;

    /// <summary>
    /// Gets or sets the maximum number of exact cases the ExactCases backend
    /// evaluates while compiling one operation. When exhausted, the
    /// remainder of the operation falls back to a conservative bound.
    /// </summary>
    public int CaseBudget { get; set; } = 4096;
}
