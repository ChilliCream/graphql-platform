using HotChocolate.Fusion.Extensions;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class ValidationHelper
{
    public static bool FieldsAreMergeable(OutputFieldDefinition[] fields)
    {
        for (var i = 0; i < fields.Length - 1; i++)
        {
            var typeA = fields[i].Type;
            var typeB = fields[i + 1].Type;

            if (!SameOutputTypeShape(typeA, typeB))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsAccessible(ArgumentAssignment _)
    {
        // FIXME: Requires support for directives on directive arguments.
        return true;
    }

    public static bool IsAccessible(Directive _)
    {
        // FIXME: Requires support for directives on directives.
        return true;
    }

    public static bool IsAccessible(IDirectivesProvider type)
    {
        return !type.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible);
    }

    public static bool SameOutputTypeShape(ITypeDefinition typeA, ITypeDefinition typeB)
    {
        var nullableTypeA = typeA.NullableType();
        var nullableTypeB = typeB.NullableType();

        if (nullableTypeA.Kind != nullableTypeB.Kind)
        {
            // Different type kind.
            return false;
        }

        if (nullableTypeA is INamedTypeDefinition namedNullableTypeA
            && nullableTypeB is INamedTypeDefinition namedNullableTypeB
            && namedNullableTypeA.Name != namedNullableTypeB.Name)
        {
            // Different type name.
            return false;
        }

        while (true)
        {
            var innerNullableTypeA = nullableTypeA.InnerNullableType();
            var innerNullableTypeB = nullableTypeB.InnerNullableType();

            // Note: InnerNullableType returns the type itself when there is no inner type.
            // "If type A has an inner type but type B does not" (or vice versa).
            if ((innerNullableTypeA != nullableTypeA && innerNullableTypeB == nullableTypeB)
                || (innerNullableTypeB != nullableTypeB && innerNullableTypeA == nullableTypeA))
            {
                // Different type depth.
                return false;
            }

            if (innerNullableTypeA == nullableTypeA)
            {
                // No more inner types.
                break;
            }

            if (innerNullableTypeA.Kind != innerNullableTypeB.Kind)
            {
                // Different type kind on inner type.
                return false;
            }

            if (innerNullableTypeA is INamedTypeDefinition namedNullableInnerTypeA
                && innerNullableTypeB is INamedTypeDefinition namedNullableInnerTypeB
                && namedNullableInnerTypeA.Name != namedNullableInnerTypeB.Name)
            {
                // Different type name on inner type.
                return false;
            }

            nullableTypeA = innerNullableTypeA;
            nullableTypeB = innerNullableTypeB;
        }

        return true;
    }
}
