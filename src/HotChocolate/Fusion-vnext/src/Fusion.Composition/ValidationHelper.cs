using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class ValidationHelper
{
    /// <summary>
    /// Returns <c>true</c> if the specified <paramref name="field"/> has a <c>@provides</c>
    /// directive that references the specified <paramref name="fieldName"/>.
    /// </summary>
    public static bool ProvidesFieldName(MutableOutputFieldDefinition field, string fieldName)
    {
        var providesDirective = field.Directives.FirstOrDefault(WellKnownDirectiveNames.Provides);

        var fieldsArgumentValueNode =
            providesDirective?.Arguments.GetValueOrDefault(WellKnownArgumentNames.Fields);

        if (fieldsArgumentValueNode is not StringValueNode fieldsArgumentStringNode)
        {
            return false;
        }

        var selectionSet =
            Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{fieldsArgumentStringNode.Value}}}");

        return selectionSet.Selections.OfType<FieldNode>().Any(f => f.Name.Value == fieldName);
    }

    public static bool SameTypeShape(IType typeA, IType typeB)
    {
        while (true)
        {
            if (typeA is NonNullType && typeB is not NonNullType)
            {
                typeA = typeA.InnerType();

                continue;
            }

            if (typeB is NonNullType && typeA is not NonNullType)
            {
                typeB = typeB.InnerType();

                continue;
            }

            if (typeA is ListType || typeB is ListType)
            {
                if (typeA is not ListType || typeB is not ListType)
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();

                continue;
            }

            return typeA.Equals(typeB, TypeComparison.Structural);
        }
    }
}
