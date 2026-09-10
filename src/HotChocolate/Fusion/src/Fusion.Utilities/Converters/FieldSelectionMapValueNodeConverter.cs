using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HC = HotChocolate.Language;

namespace HotChocolate.Fusion.Converters;

/// <summary>
/// Converts <see cref="Language"/> constant value nodes (as used in a field
/// selection map) into the equivalent <see cref="HotChocolate.Language"/> value nodes.
/// </summary>
public static class FieldSelectionMapValueNodeConverter
{
    public static HC.IValueNode Convert(IValueNode node)
    {
        return node switch
        {
            StringValueNode value => new HC.StringValueNode(null, value.Value, value.Block),
            // HotChocolate.Language.IntValueNode has no string constructor; parse the literal.
            IntValueNode value => HC.Utf8GraphQLParser.Syntax.ParseValueLiteral(value.Value, constant: true),
            FloatValueNode value => HC.Utf8GraphQLParser.Syntax.ParseValueLiteral(value.Value, constant: true),
            BooleanValueNode value => new HC.BooleanValueNode(value.Value),
            EnumValueNode value => new HC.EnumValueNode(value.Value),
            NullValueNode => HC.NullValueNode.Default,
            ListValueNode value => new HC.ListValueNode(value.Items.Select(Convert).ToArray()),
            ObjectValueNode value => new HC.ObjectValueNode(value.Fields.Select(Convert).ToArray()),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
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
