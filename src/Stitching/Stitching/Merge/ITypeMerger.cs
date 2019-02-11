using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public interface ITypeMerger
    {
        void Merge(
            IMergeSchemaContext context,
            IReadOnlyList<ITypeInfo> types);
    }
}
