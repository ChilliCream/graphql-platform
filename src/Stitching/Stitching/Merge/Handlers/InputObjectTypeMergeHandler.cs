using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class InputObjectTypeMergeHandler
        : TypeMergeHanlderBase<InputObjectTypeInfo>
    {
        public InputObjectTypeMergeHandler(MergeTypeRuleDelegate next)
            : base(next)
        {
        }

        protected override void MergeTypes(
            ISchemaMergeContext context,
            IReadOnlyList<InputObjectTypeInfo> types,
            NameString newTypeName)
        {
            List<InputObjectTypeDefinitionNode> definitions = types
                .Select(t => t.Definition)
                .ToList();

            InputObjectTypeDefinitionNode definition =
                definitions[0].Rename(
                    newTypeName,
                    types.Select(t => t.Schema.Name));

            context.AddType(definition);
        }

        protected override bool CanBeMerged(
            InputObjectTypeInfo left,
            InputObjectTypeInfo right)
        {
            var processed = new HashSet<string>();
            var queue = new Queue<TypePair>();
            var fieldTypes = new List<string>();

            queue.Enqueue(new TypePair(left, right));

            while (queue.Count > 0)
            {
                TypePair pair = queue.Dequeue();
                processed.Add(pair.Left.Definition.Name.Value);
                fieldTypes.Clear();

                if (pair.Left.Definition is InputObjectTypeDefinitionNode ld
                    && pair.Right.Definition is InputObjectTypeDefinitionNode rd
                    && CanBeMerged(ld, rd, fieldTypes)
                    && CanHandleFieldTypes(pair, fieldTypes, processed, queue))
                {
                    processed.Add(ld.Name.Value);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanHandleFieldTypes(
            TypePair typePair,
            ICollection<string> fieldTypes,
            ISet<string> processed,
            Queue<TypePair> queue)
        {
            if (fieldTypes.Count > 0)
            {
                foreach (string typeName in fieldTypes)
                {
                    if (processed.Add(typeName)
                        && !TryEnqueueFieldType(typePair, typeName, queue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryEnqueueFieldType(
            TypePair typePair,
            string typeName,
            Queue<TypePair> queue)
        {
            if (typePair.Left.Schema.Types.TryGetValue(typeName,
                out ITypeDefinitionNode lt)
                && typePair.Right.Schema.Types.TryGetValue(typeName,
                out ITypeDefinitionNode rt))
            {
                return TryEnqueueForAnalysis(
                    TypeInfo.Create(lt, typePair.Left.Schema),
                    TypeInfo.Create(rt, typePair.Right.Schema),
                    queue);
            }
            else if (!typePair.Left.Schema.Types.ContainsKey(typeName)
                && !typePair.Right.Schema.Types.ContainsKey(typeName))
            {
                // if the type does not exist in either schema then we expect
                // it to be a scalar type.
                return true;
            }

            return false;
        }

        private static bool TryEnqueueForAnalysis(
            ITypeInfo left, ITypeInfo right,
            Queue<TypePair> queue)
        {
            switch (GetMergeStatus(left.Definition, left.Definition))
            {
                case MergeStatus.Analyze:
                    queue.Enqueue(new TypePair(right, left));
                    return true;
                case MergeStatus.Merge:
                    return true;
                default:
                    return false;
            }
        }

        private static MergeStatus GetMergeStatus(
            ITypeDefinitionNode leftType,
            ITypeDefinitionNode rightType)
        {
            if (leftType is InputObjectTypeDefinitionNode
                && rightType is InputObjectTypeDefinitionNode)
            {
                return MergeStatus.Analyze;
            }
            else if (leftType is ScalarTypeDefinitionNode
                && rightType is ScalarTypeDefinitionNode)
            {
                return MergeStatus.Merge;
            }
            else if (leftType is EnumTypeDefinitionNode let
                && rightType is EnumTypeDefinitionNode ret)
            {
                return EnumTypeMergeHandler.CanBeMerged(let, ret)
                    ? MergeStatus.Merge
                    : MergeStatus.Invalid;
            }

            return MergeStatus.Analyze;
        }

        private static bool CanBeMerged(
            InputObjectTypeDefinitionNode left,
            InputObjectTypeDefinitionNode right,
            ICollection<string> typesToAnalyze)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && left.Fields.Count == right.Fields.Count)
            {
                Dictionary<string, InputValueDefinitionNode> leftArgs =
                    left.Fields.ToDictionary(t => t.Name.Value);
                Dictionary<string, InputValueDefinitionNode> rightArgs =
                    left.Fields.ToDictionary(t => t.Name.Value);

                foreach (string name in leftArgs.Keys)
                {
                    InputValueDefinitionNode leftArgument = leftArgs[name];
                    if (!rightArgs.TryGetValue(name,
                        out InputValueDefinitionNode rightArgument)
                        || !HasSameType(
                            leftArgument.Type,
                            rightArgument.Type,
                            typesToAnalyze))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HasSameType(
            ITypeNode left,
            ITypeNode right,
            ICollection<string> typesToAnalyze)
        {
            if (left is NonNullTypeNode lnntn
                && right is NonNullTypeNode rnntn)
            {
                return HasSameType(lnntn.Type, rnntn.Type, typesToAnalyze);
            }

            if (left is ListTypeNode lltn
                && right is ListTypeNode rltn)
            {
                return HasSameType(lltn.Type, rltn.Type, typesToAnalyze);
            }

            if (left is NamedTypeNode lntn
                && right is NamedTypeNode rntn)
            {

                if (lntn.Name.Value.Equals(
                    rntn.Name.Value,
                    StringComparison.Ordinal))
                {
                    typesToAnalyze.Add(rntn.Name.Value);
                    return true;
                }
                return false;
            }

            throw new NotSupportedException();
        }

        private class TypePair
        {
            public TypePair(
                ITypeInfo left,
                ITypeInfo right)
            {
                Left = left;
                Right = right;
            }

            public ITypeInfo Left { get; }

            public ITypeInfo Right { get; }
        }

        private enum MergeStatus
        {
            Merge,
            Invalid,
            Analyze
        }
    }
}
