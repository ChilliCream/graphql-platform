namespace HotChocolate.CostAnalysis;

/// <summary>
/// Configures the schema-level behavior of cost analysis.
/// </summary>
public sealed class CostSchemaIndexOptions
{
    /// <summary>
    /// Gets or sets the assumed size for list fields without applicable list-size information.
    /// The default is <see cref="double.PositiveInfinity"/>.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double DefaultListSize
    {
        get;
        set
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(DefaultListSize), value);
            }

            field = value;
        }
    } = double.PositiveInfinity;

    /// <summary>
    /// Gets or sets the maximum number of Boolean case splits allowed when compiling an operation.
    /// The default is 510. A value of zero or less uses
    /// <see cref="CaseBudgetExceededBehavior"/> immediately.
    /// </summary>
    public int CaseBudget { get; set; } = 510;

    /// <summary>
    /// Gets or sets the behavior once compiling one operation exhausts
    /// <see cref="CaseBudget"/>. The default is
    /// <see cref="CaseBudgetExceededBehavior.EvaluatePerRequest"/>.
    /// </summary>
    public CaseBudgetExceededBehavior CaseBudgetExceededBehavior { get; set; }
        = CaseBudgetExceededBehavior.EvaluatePerRequest;
}
