using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class ScalarTypeMergeHandler
        : ITypeMergeHandler
    {
        private readonly MergeTypeRuleDelegate _next;

        public ScalarTypeMergeHandler(MergeTypeRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            IReadOnlyList<ITypeInfo> unhandled =
                types.OfType<ScalarTypeInfo>().Any()
                    ? types.NotOfType<ScalarTypeInfo>()
                    : types;

            _next.Invoke(context, unhandled);
        }
    }
}
