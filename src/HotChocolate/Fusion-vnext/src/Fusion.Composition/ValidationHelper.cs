using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class ValidationHelper
{
    /// <summary>
    /// Returns <c>true</c> if the specified <paramref name="field"/> has a <c>@provides</c>
    /// directive that references the specified <paramref name="fieldName"/>.
    /// </summary>
    public static bool ProvidesFieldName(OutputFieldDefinition field, string fieldName)
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
