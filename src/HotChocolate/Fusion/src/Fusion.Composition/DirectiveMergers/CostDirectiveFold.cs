using HotChocolate.Types;

namespace HotChocolate.Fusion.DirectiveMergers;

/// <summary>
/// Computes the coordinate-kind default weight and the weight fold used to derive the public
/// <c>@cost</c> directive from the effective weight of every serving source.
/// </summary>
internal static class CostDirectiveFold
{
    /// <summary>
    /// The spec default weight for a composite (object, interface, or union) type.
    /// </summary>
    public const double CompositeTypeDefaultWeight = 1;

    /// <summary>
    /// The spec default weight for a leaf (scalar or enum) type.
    /// </summary>
    public const double LeafTypeDefaultWeight = 0;

    /// <summary>
    /// Gets the default weight for an output field: 1 when its named type (the list element
    /// type for a list field) is a composite type, 0 otherwise.
    /// </summary>
    public static double GetOutputFieldDefaultWeight(IType fieldType)
        => IsComposite(fieldType.NamedType()) ? 1 : 0;

    /// <summary>
    /// Gets the default weight for an argument or input field: 1 when its named type is an
    /// input object type, 0 otherwise.
    /// </summary>
    public static double GetInputValueDefaultWeight(IType inputType)
        => inputType.NamedType().Kind == TypeKind.InputObject ? 1 : 0;

    /// <summary>
    /// Folds the effective weight of every serving source into the public weight: the maximum
    /// over all sources, a true upper bound over whichever source serves the coordinate.
    /// </summary>
    public static double FoldWeights(IReadOnlyList<double> effectiveWeights)
    {
        var max = double.NegativeInfinity;

        foreach (var weight in effectiveWeights)
        {
            if (weight > max)
            {
                max = weight;
            }
        }

        return max;
    }

    private static bool IsComposite(ITypeDefinition namedType)
        => namedType.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union;
}
