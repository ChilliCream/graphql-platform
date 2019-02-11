using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public class ObjectTypeMergeHandler
         : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public ObjectTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {

        }
    }
}
