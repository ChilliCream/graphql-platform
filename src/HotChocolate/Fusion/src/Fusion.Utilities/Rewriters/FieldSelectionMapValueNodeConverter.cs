using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HC = HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

/// <summary>
/// Converts <see cref="HotChocolate.Fusion.Language"/> constant value nodes (as used in a field
/// selection map) into the equivalent <see cref="HotChocolate.Language"/> value nodes.
/// </summary>
public static class FieldSelectionMapValueNodeConverter
{
    public static HC.IValueNode Convert(IValueNode node)
    {
        switch (node)
        {
            case StringValueNode value:
                return new HC.StringValueNode(null, value.Value, value.Block);

            case IntValueNode value:
                // HotChocolate.Language.IntValueNode has no string constructor; parse the literal.
                return HC.Utf8GraphQLParser.Syntax.ParseValueLiteral(value.Value, /* constant: */ true);

            case FloatValueNode value:
                return HC.Utf8GraphQLParser.Syntax.ParseValueLiteral(value.Value, /* constant: */ true);

            case BooleanValueNode value:
                return new HC.BooleanValueNode(value.Value);

            case EnumValueNode value:
                return new HC.EnumValueNode(value.Value);

            case NullValueNode:
                return HC.NullValueNode.Default;

            case ListValueNode value:
                return new HC.ListValueNode(value.Items.Select(Convert).ToArray());

            case ObjectValueNode value:
                return new HC.ObjectValueNode(value.Fields.Select(Convert).ToArray());

            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    public static HC.ArgumentNode Convert(ArgumentNode argument)
        => new(argument.Name.Value, Convert(argument.Value));

    public static IReadOnlyList<HC.ArgumentNode> Convert(ImmutableArray<ArgumentNode> arguments)
        => arguments.IsDefaultOrEmpty
            ? []
            : arguments.Select(Convert).ToArray();

    private static HC.ObjectFieldNode Convert(ObjectFieldNode field)
        => new(field.Name.Value, Convert(field.Value));
}
