using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A minimal IBM-cost-shaped <see cref="IAnalysisAlgebra{T}"/> test double
/// for the Traversal test suite. Reads <c>@cost</c> weights straight off
/// the field and type definitions <see cref="CollectedFieldGroup"/> carries
/// and applies the field rule, ignoring argument and directive-argument
/// costs.
/// </summary>
internal sealed class TestCostAlgebra : IAnalysisAlgebra<(double TypeCost, double FieldCost)>
{
    public (double TypeCost, double FieldCost) Empty => (0, 0);

    public (double TypeCost, double FieldCost) Field(in CollectedFieldGroup group, (double TypeCost, double FieldCost) child)
    {
        var typeWeight = double.NegativeInfinity;
        var fieldWeight = double.NegativeInfinity;

        foreach (var member in group.Members)
        {
            var returnType = member.Field.Type.NamedType();
            typeWeight = Math.Max(typeWeight, ReadWeight(returnType.Directives, DefaultWeight(returnType.Kind)));
            fieldWeight = Math.Max(fieldWeight, ReadWeight(member.Field.Directives, DefaultWeight(returnType.Kind)));
        }

        var n = group.ListMultiplier;
        var typeCost = ClampZero(n * (typeWeight + child.TypeCost));
        var fieldCost = ClampZero(fieldWeight) + n * child.FieldCost;
        return (typeCost, fieldCost);
    }

    public (double TypeCost, double FieldCost) Combine(
        (double TypeCost, double FieldCost) left,
        (double TypeCost, double FieldCost) right)
        => (left.TypeCost + right.TypeCost, left.FieldCost + right.FieldCost);

    public (double TypeCost, double FieldCost) Join(
        (double TypeCost, double FieldCost) left,
        (double TypeCost, double FieldCost) right)
        => (Math.Max(left.TypeCost, right.TypeCost), Math.Max(left.FieldCost, right.FieldCost));

    private static double DefaultWeight(TypeKind kind)
        => kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union ? 1.0 : 0.0;

    private static double ReadWeight(IReadOnlyDirectiveCollection directives, double defaultWeight)
    {
        var directive = directives.FirstOrDefault(DirectiveNames.Cost.Name);

        if (directive is null)
        {
            return defaultWeight;
        }

        directive.Arguments.TryGetValue(DirectiveNames.Cost.Arguments.Weight, out var value);

        return value switch
        {
            IntValueNode intValue => intValue.ToDouble(),
            FloatValueNode floatValue => floatValue.ToDouble(),
            StringValueNode stringValue => double.Parse(stringValue.Value, CultureInfo.InvariantCulture),
            _ => defaultWeight
        };
    }

    private static double ClampZero(double value) => value < 0 ? 0 : value;
}
