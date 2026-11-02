namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes field and type costs from field weights, argument costs, and list sizes.
/// </summary>
/// <remarks>
/// All members accept any <see cref="double"/> value and do not throw.
/// </remarks>
internal static class CostFieldRule
{
    /// <summary>
    /// Gets the identity element of <see cref="Combine"/>.
    /// </summary>
    public static CostEstimate Empty { get; } = new(0.0, 0.0, null);

    /// <summary>
    /// Adds the field costs and type costs of two estimates.
    /// </summary>
    public static CostEstimate Combine(CostEstimate left, CostEstimate right)
        => new(left.FieldCost + right.FieldCost, left.TypeCost + right.TypeCost, null);

    /// <summary>
    /// Takes the larger field cost and the larger type cost from two alternative estimates.
    /// A numeric value takes precedence over <see cref="double.NaN"/>.
    /// </summary>
    public static CostEstimate Join(CostEstimate left, CostEstimate right)
        => new(
            double.MaxNumber(left.FieldCost, right.FieldCost),
            double.MaxNumber(left.TypeCost, right.TypeCost),
            null);

    /// <summary>
    /// Multiplies <paramref name="cost"/> by <paramref name="n"/>.
    /// A zero cost returns zero, including when <paramref name="n"/> is infinite.
    /// </summary>
    public static double Scale(double n, double cost) => cost == 0.0 ? 0.0 : n * cost;

    /// <summary>
    /// Returns zero for a negative value or <see cref="double.NaN"/>; otherwise returns the value.
    /// </summary>
    public static double Clamp0(double value) => double.MaxNumber(value, 0.0);

    /// <summary>
    /// Computes one field call's contribution to its selection boundary.
    /// </summary>
    /// <param name="n">
    /// The list multiplier in effect for this call, 1.0 for a non-list
    /// field.
    /// </param>
    /// <param name="fieldWeight">
    /// The field's own weight.
    /// </param>
    /// <param name="argumentsCost">
    /// The field's arguments' combined cost, paid once regardless of
    /// <paramref name="n"/>.
    /// </param>
    /// <param name="directiveArgumentsCost">
    /// The combined cost of the arguments of the directives applied to this
    /// field call, paid once regardless of <paramref name="n"/>.
    /// </param>
    /// <param name="returnTypeWeight">
    /// The field's return type's own weight: the signed max over possible
    /// object types when the return type is abstract.
    /// </param>
    /// <param name="child">
    /// The combined estimate of the field's own selection set.
    /// </param>
    /// <returns>
    /// The field call's estimate. Clamping applies only to the whole
    /// field-call sum and to the whole per-instance type cost, never to an
    /// intermediate term.
    /// </returns>
    public static CostEstimate Field(
        double n,
        double fieldWeight,
        double argumentsCost,
        double directiveArgumentsCost,
        double returnTypeWeight,
        CostEstimate child)
    {
        var typeCost = Clamp0(Scale(n, returnTypeWeight + child.TypeCost));
        var fieldCost = Clamp0(fieldWeight + argumentsCost + directiveArgumentsCost) + Scale(n, child.FieldCost);
        return new CostEstimate(fieldCost, typeCost, null);
    }

    /// <summary>
    /// Adds an operation's root type weight to its root selection's
    /// estimate. The root's field cost carries no root weight, only its
    /// type cost does.
    /// </summary>
    /// <param name="rootTypeWeight">
    /// The operation's root type's own weight.
    /// </param>
    /// <param name="selection">
    /// The combined estimate of the operation's root selection set.
    /// </param>
    public static CostEstimate Root(double rootTypeWeight, CostEstimate selection)
        => new(selection.FieldCost, Clamp0(rootTypeWeight + selection.TypeCost), null);
}
