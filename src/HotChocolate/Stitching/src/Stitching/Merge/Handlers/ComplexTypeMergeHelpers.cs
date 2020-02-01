using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal static class ComplexTypeMergeHelpers
    {
        public static bool CanBeMergedWith(
            this InterfaceTypeInfo left,
            InterfaceTypeInfo right)
        {
            return CanBeMerged(left.Definition, right.Definition);
        }

        public static bool CanBeMergedWith(
            this ObjectTypeInfo left,
            ObjectTypeInfo right)
        {
            return CanBeMerged(left.Definition, right.Definition);
        }

        private static bool CanBeMerged(
            ComplexTypeDefinitionNodeBase left,
            ComplexTypeDefinitionNodeBase right)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && left.Fields.Count == right.Fields.Count)
            {
                Dictionary<string, FieldDefinitionNode> leftFields =
                    left.Fields.ToDictionary(t => t.Name.Value);
                Dictionary<string, FieldDefinitionNode> rightFields =
                    right.Fields.ToDictionary(t => t.Name.Value);

                foreach (string name in leftFields.Keys)
                {
                    FieldDefinitionNode leftField = leftFields[name];
                    if (!rightFields.TryGetValue(name,
                        out FieldDefinitionNode rightField)
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
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
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

            foreach (string name in leftArgs.Keys)
            {
                InputValueDefinitionNode leftArgument = leftArgs[name];
                if (!rightArgs.TryGetValue(name,
                    out InputValueDefinitionNode rightArgument)
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
                return lntn.Name.Value.Equals(
                    rntn.Name.Value,
                    StringComparison.Ordinal);
            }

            return false;
        }
    }
}
