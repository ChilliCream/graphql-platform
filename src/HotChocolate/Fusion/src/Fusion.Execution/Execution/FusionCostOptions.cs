namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the cost analysis options for the Fusion gateway.
/// </summary>
public sealed class FusionCostOptions
{
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the maximum allowed field cost.
    /// <c>1000</c> by default.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxFieldCost
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxFieldCost), value);
            }

            field = value;
        }
    } = 1_000;

    /// <summary>
    /// Gets or sets the maximum allowed type cost.
    /// <c>1000</c> by default.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxTypeCost
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxTypeCost), value);
            }

            field = value;
        }
    } = 1_000;

    /// <summary>
    /// Gets or sets whether the cost analyzer enforces <see cref="MaxFieldCost"/>
    /// and <see cref="MaxTypeCost"/>. <c>true</c> by default.
    /// </summary>
    public bool EnforceCostLimits
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = true;

    /// <summary>
    /// Gets or sets whether the cost analyzer is skipped entirely.
    /// <c>false</c> by default.
    /// </summary>
    public bool SkipAnalyzer
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowed response size. <c>null</c> disables the check.
    /// <c>null</c> by default.
    /// A non-null value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double? MaxResponseSize
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (value is { } size && (double.IsNaN(size) || size < 0))
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxResponseSize), size);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the assumed size of a list field that has no applicable
    /// <c>@listSize</c> information. <see cref="double.PositiveInfinity"/> by default.
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
            ExpectMutableOptions();

            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(DefaultListSize), value);
            }

            field = value;
        }
    } = double.PositiveInfinity;

    /// <summary>
    /// Gets or sets the maximum number of compiled cost plans cached per schema.
    /// <c>256</c> by default.
    /// </summary>
    public int CostPlanCacheSize
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = 256;

    /// <summary>
    /// Gets or sets the maximum number of exact cases the cost engine evaluates
    /// for a single operation. <c>null</c> uses the cost engine's default budget.
    /// </summary>
    public int? CaseBudget
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Clones the cost options into a new mutable instance.
    /// </summary>
    /// <returns>
    /// A new mutable instance of <see cref="FusionCostOptions"/> with the same properties.
    /// </returns>
    public FusionCostOptions Clone()
    {
        return new FusionCostOptions
        {
            MaxFieldCost = MaxFieldCost,
            MaxTypeCost = MaxTypeCost,
            EnforceCostLimits = EnforceCostLimits,
            SkipAnalyzer = SkipAnalyzer,
            MaxResponseSize = MaxResponseSize,
            DefaultListSize = DefaultListSize,
            CostPlanCacheSize = CostPlanCacheSize,
            CaseBudget = CaseBudget
        };
    }

    internal void MakeReadOnly()
        => _isReadOnly = true;

    private void ExpectMutableOptions()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The cost options are read-only.");
        }
    }
}
