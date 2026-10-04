namespace HotChocolate.CostAnalysis;

/// <summary>
/// One signed reference to a Boolean variable in a canonical condition:
/// positive for <c>@include(if:$x)</c>, negative for <c>@skip(if:$x)</c>.
/// </summary>
internal readonly record struct BooleanLiteral(string VariableName, bool IsPositive)
{
    /// <summary>
    /// Gets the opposite literal for the same variable.
    /// </summary>
    public BooleanLiteral Complement => this with { IsPositive = !IsPositive };

    /// <inheritdoc />
    public override string ToString() => (IsPositive ? "+" : "-") + VariableName;
}
