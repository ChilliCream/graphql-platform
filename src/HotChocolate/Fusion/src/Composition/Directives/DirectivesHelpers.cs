using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

internal static class DirectivesHelpers
{
    public static BooleanValueNode ExpectBooleanValue(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);

        if (valueNode is BooleanValueNode booleanValue)
        {
            return booleanValue;
        }

        throw new ArgumentException(
            CompositionResources.DirectiveHelpers_ExpectBooleanLiteral_Message,
            nameof(valueNode));
    }

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
    
    public static EnumValueNode ExpectEnumLiteral(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);

        if (valueNode is EnumValueNode enumValue)
        {
            return enumValue;
        }

        throw new ArgumentException(
            CompositionResources.DirectivesHelpers_ExpectEnumLiteral_Message,
            nameof(valueNode));
    }

    public static FieldNode ExpectFieldSelection(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);
        var value = valueNode.ExpectStringLiteral();
        return Utf8GraphQLParser.Syntax.ParseField(value.AsSpan());
    }

    public static SelectionSetNode ExpectSelectionSet(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);
        var value = valueNode.ExpectStringLiteral();
        return Utf8GraphQLParser.Syntax.ParseSelectionSet(value.AsSpan());
    }
    
    public static OperationDefinitionNode ExpectOperationDefinition(this IValueNode valueNode)
    {
        ArgumentNullException.ThrowIfNull(valueNode);
        var value = valueNode.ExpectStringLiteral();
        return Utf8GraphQLParser.Syntax.ParseOperationDefinition(value.AsSpan());
    }
}