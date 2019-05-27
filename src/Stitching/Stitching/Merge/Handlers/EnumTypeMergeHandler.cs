using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class EnumTypeMergeHandler
        : ITypeMergeHandler
    {
        private readonly MergeTypeRuleDelegate _next;

        public EnumTypeMergeHandler(MergeTypeRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.OfType<EnumTypeInfo>().Any())
            {
                var notMerged = types.OfType<EnumTypeInfo>().ToList();
                bool hasLeftovers = types.Count > notMerged.Count;

                while (notMerged.Count > 0)
                {
                    MergeNextType(context, notMerged);
                }

                if (hasLeftovers)
                {
                    _next.Invoke(context, types.NotOfType<EnumTypeInfo>());
                }
            }
            else
            {
                _next.Invoke(context, types);
            }
        }

        private static void MergeNextType(
            ISchemaMergeContext context,
            List<EnumTypeInfo> notMerged)
        {
            EnumTypeInfo left = notMerged[0];

            var leftValueSet = new HashSet<string>(
            left.Definition.Values.Select(t => t.Name.Value));

            var readyToMerge = new List<EnumTypeInfo>();
            readyToMerge.Add(left);

            for (int i = 1; i < notMerged.Count; i++)
            {
                if (CanBeMerged(leftValueSet, notMerged[i].Definition))
                {
                    readyToMerge.Add(notMerged[i]);
                }
            }

            MergeType(context, readyToMerge);
            notMerged.RemoveAll(readyToMerge.Contains);
        }

        private static void MergeType(
            ISchemaMergeContext context,
            IReadOnlyList<EnumTypeInfo> types)
        {
            var definition = types[0].Definition;

            EnumTypeDefinitionNode descriptionDef =
                types.Select(t => t.Definition)
                .FirstOrDefault(t => t.Description != null);

            if (descriptionDef != null)
            {
                definition = definition.WithDescription(
                    descriptionDef.Description);
            }

            context.AddType(definition.Rename(
                TypeMergeHelpers.CreateName(context, types),
                types.Select(t => t.Schema.Name)));
        }

        internal static bool CanBeMerged(
            EnumTypeDefinitionNode left,
            EnumTypeDefinitionNode right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            var leftValueSet = new HashSet<string>(
                left.Values.Select(t => t.Name.Value));
            return CanBeMerged(leftValueSet, right);
        }

        private static bool CanBeMerged(
            ICollection<string> left,
            EnumTypeDefinitionNodeBase right)
        {
            if (left.Count == right.Values.Count)
            {
                for (int i = 0; i < right.Values.Count; i++)
                {
                    if (!left.Contains(right.Values[i].Name.Value))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
