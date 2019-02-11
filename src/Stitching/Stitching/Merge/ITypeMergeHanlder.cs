using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public interface ITypeMergeHanlder
    {
        void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types);
    }
}
