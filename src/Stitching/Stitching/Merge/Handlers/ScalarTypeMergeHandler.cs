using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class ScalarTypeMergeHandler
        : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public ScalarTypeMergeHandler(MergeTypeDelegate next)
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
