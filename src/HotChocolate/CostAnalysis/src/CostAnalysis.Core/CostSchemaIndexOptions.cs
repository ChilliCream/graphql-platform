namespace HotChocolate.CostAnalysis;

/// <summary>
/// Configures the schema-level behavior of cost analysis.
/// </summary>
public sealed class CostSchemaIndexOptions
{
    /// <summary>
    /// Gets or sets the list size used for a list-typed field that carries no
    /// <c>@listSize</c> annotation and no slicing arguments. The default is
    /// <see cref="double.PositiveInfinity"/>.
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
    /// Gets or sets the maximum number of exact splits evaluated while
    /// compiling one operation before the remainder falls back to a
    /// conservative bound. The default is 510.
    /// </summary>
    public int CaseBudget { get; set; } = 510;
}
