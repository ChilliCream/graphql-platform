using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class InputUnionTypeMergeHandler
        : ITypeMergeHandler
    {
        private readonly MergeTypeRuleDelegate _next;

        public InputUnionTypeMergeHandler(MergeTypeRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.OfType<InputUnionTypeInfo>().Any())
            {
                var notMerged = types.OfType<InputUnionTypeInfo>().ToList();
                bool hasLeftovers = types.Count > notMerged.Count;

                for (int i = 0; i < notMerged.Count; i++)
                {
                    context.AddType(notMerged[i].Definition.Rename(
                        TypeMergeHelpers.CreateName(context, notMerged[i]),
                        notMerged[i].Schema.Name));
                }

                if (hasLeftovers)
                {
                    _next.Invoke(context, types.NotOfType<InputUnionTypeInfo>());
                }
            }
            else
            {
                _next.Invoke(context, types);
            }
        }
    }
}
