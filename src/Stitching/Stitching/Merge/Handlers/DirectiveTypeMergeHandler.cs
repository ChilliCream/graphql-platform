using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class DirectiveTypeMergeHandler
    {
        private readonly MergeDirectiveRuleDelegate _next;

        public DirectiveTypeMergeHandler(MergeDirectiveRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<IDirectiveTypeInfo> types)
        {
            var notMerged = types.ToList();

            while (notMerged.Count > 0)
            {
                MergeNextType(context, notMerged);
            }
        }

        private void MergeNextType(
           ISchemaMergeContext context,
           List<IDirectiveTypeInfo> notMerged)
        {
            IDirectiveTypeInfo left = notMerged[0];

            var readyToMerge = new List<IDirectiveTypeInfo>();
            readyToMerge.Add(left);

            for (int i = 1; i < notMerged.Count; i++)
            {
                if (CanBeMerged(left.Definition, notMerged[i].Definition))
                {
                    readyToMerge.Add(notMerged[i]);
                }
            }

            NameString name = readyToMerge[0].Definition.Name.Value;

            if (context.ContainsDirective(name))
            {
                throw new InvalidOperationException($"Unable to merge {name}, directive with this name already exists.");
            }

            MergeTypes(context, readyToMerge, name);
            notMerged.RemoveAll(readyToMerge.Contains);
        }

        protected void MergeTypes(
            ISchemaMergeContext context,
            IReadOnlyList<IDirectiveTypeInfo> types,
            NameString newTypeName)
        {
            var definitions = types
                .Select(t => t.Definition)
                .ToList();

            DirectiveDefinitionNode definition =
                definitions[0].Rename(
                    newTypeName,
                    types.Select(t => t.Schema.Name));

            context.AddDirective(definition);
        }

        private static bool CanBeMerged(DirectiveDefinitionNode left, DirectiveDefinitionNode right)
        {
            if (!left.Name.Value.Equals(right.Name.Value, StringComparison.Ordinal))
            {
                return false;
            }

            if (left.Locations.Count != right.Locations.Count)
            {
                return false;
            }

            var leftLocations = left.Locations.Select(l => l.Value).OrderBy(l => l).ToList();
            var rightLocations = right.Locations.Select(l => l.Value).OrderBy(l => l).ToList();

            if (!leftLocations.SequenceEqual(rightLocations))
            {
                return false;
            }

            if (left.IsRepeatable != right.IsRepeatable)
            {
                return false;
            }

            if (left.Arguments.Count != right.Arguments.Count)
            {
                return false;
            }

            return ComplexTypeMergeHelpers.HasSameArguments(left.Arguments, right.Arguments);
        }
    }
}
