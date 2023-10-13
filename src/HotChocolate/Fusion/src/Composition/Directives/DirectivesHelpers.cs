using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

internal static class DirectivesHelpers
{
    public static StringValueNode ExpectStringLiteral(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);

        if (valueNode is StringValueNode stringLiteral)
        {
            return stringLiteral;
        }

        throw new ArgumentException(
            CompositionResources.DirectiveHelpers_ExpectStringLiteral_Message,
            nameof(valueNode));
    }
    
    public static StringValueNode? ExpectStringOrNullLiteral(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);

        if (valueNode is StringValueNode stringLiteral)
        {
            return stringLiteral;
        }

        if (valueNode.Kind is SyntaxKind.NullValue)
        {
            return null;
        }

        throw new ArgumentException(
            CompositionResources.DirectiveHelpers_ExpectStringLiteral_Message,
            nameof(valueNode));
    }
    
    public static FieldNode ExpectFieldSelection(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);
        var value = valueNode.ExpectStringLiteral();
        return Utf8GraphQLParser.Syntax.ParseField(value.AsSpan());
    }
}