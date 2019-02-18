using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    public interface ITypeMergeHanlder
    {
        void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types);
    }
}
