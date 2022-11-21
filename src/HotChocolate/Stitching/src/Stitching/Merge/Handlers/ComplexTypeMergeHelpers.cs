using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Handlers;

internal static class ComplexTypeMergeHelpers
{
    public static bool CanBeMergedWith(
        this InterfaceTypeInfo left,
        InterfaceTypeInfo right)
        => CanBeMerged(left.Definition, right.Definition);

    public static bool CanBeMergedWith(
        this ObjectTypeInfo left,
        ObjectTypeInfo right)
        => CanBeMerged(left.Definition, right.Definition);

    private static bool CanBeMerged(
        ComplexTypeDefinitionNodeBase left,
        ComplexTypeDefinitionNodeBase right)
    {
        if (left.Name.Value.EqualsOrdinal(right.Name.Value)
            && left.Fields.Count == right.Fields.Count)
        {
            var leftFields = left.Fields.ToDictionary(t => t.Name.Value);
            var rightFields = right.Fields.ToDictionary(t => t.Name.Value);

            foreach (var name in leftFields.Keys)
            {
                var leftField = leftFields[name];
                if (!rightFields.TryGetValue(name, out var rightField)
                    || !HasSameShape(leftField, rightField))
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    private static bool HasSameShape(
        FieldDefinitionNode left,
        FieldDefinitionNode right)
    {
        if (left.Name.Value.EqualsOrdinal(right.Name.Value)
            && HasSameType(left.Type, right.Type)
            && left.Arguments.Count == right.Arguments.Count)
        {
            return HasSameArguments(left.Arguments, right.Arguments);
        }
        return false;
    }

    public static bool HasSameArguments(
        IReadOnlyList<InputValueDefinitionNode> left,
        IReadOnlyList<InputValueDefinitionNode> right)
    {
        var leftArgs = left.ToDictionary(t => t.Name.Value);
        var rightArgs = right.ToDictionary(t => t.Name.Value);

        foreach (var name in leftArgs.Keys)
        {
            var leftArgument = leftArgs[name];
            if (!rightArgs.TryGetValue(name, out var rightArgument)
                || !HasSameType(leftArgument.Type, rightArgument.Type))
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasSameType(ITypeNode left, ITypeNode right)
    {
        if (left is NonNullTypeNode lnntn
            && right is NonNullTypeNode rnntn)
        {
            return HasSameType(lnntn.Type, rnntn.Type);
        }

        if (left is ListTypeNode lltn
            && right is ListTypeNode rltn)
        {
            return HasSameType(lltn.Type, rltn.Type);
        }

        if (left is NamedTypeNode lntn
            && right is NamedTypeNode rntn)
        {
            return lntn.Name.Value.EqualsOrdinal(rntn.Name.Value);
        }

        return false;
    }
}
