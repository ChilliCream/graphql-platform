using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    public interface ITypeMergeHandler
    {
        void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types);
    }
}
