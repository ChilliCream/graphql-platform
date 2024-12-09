using System.Collections.Immutable;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class ValidationHelper
{
    public static bool FieldsAreMergeable(ImmutableArray<OutputFieldDefinition> fields)
    {
        for (var i = 0; i < fields.Length - 1; i++)
        {
            var typeA = fields[i].Type;
            var typeB = fields[i + 1].Type;

            if (!SameTypeShape(typeA, typeB))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsAccessible(IDirectivesProvider type)
    {
        return !type.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible);
    }

    public static bool SameTypeShape(ITypeDefinition typeA, ITypeDefinition typeB)
    {
        while (true)
        {
            if (typeA is NonNullTypeDefinition && typeB is not NonNullTypeDefinition)
            {
                typeA = typeA.InnerType();

                continue;
            }

            if (typeB is NonNullTypeDefinition && typeA is not NonNullTypeDefinition)
            {
                typeB = typeB.InnerType();

                continue;
            }

            if (typeA is ListTypeDefinition || typeB is ListTypeDefinition)
            {
                if (typeA is not ListTypeDefinition || typeB is not ListTypeDefinition)
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();

                continue;
            }

            if (typeA.Kind != typeB.Kind)
            {
                return false;
            }

            if (typeA.NamedType().Name != typeB.NamedType().Name)
            {
                return false;
            }

            return true;
        }
    }
}
