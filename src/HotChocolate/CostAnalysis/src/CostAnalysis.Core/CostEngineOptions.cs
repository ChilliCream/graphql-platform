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
    /// Gets or sets the maximum number of exact splits evaluated while
    /// compiling one operation before the remainder falls back to a
    /// conservative bound. The default is 510, the measured K8
    /// full-operation boundary at revision <c>6c9945cc29</c>.
    /// </summary>
    public int CaseBudget { get; set; } = 510;
}
