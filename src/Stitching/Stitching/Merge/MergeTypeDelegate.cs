using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    public delegate MergeTypeDelegate MergeTypeHandler(
        MergeTypeDelegate next);

    public delegate void MergeTypeDelegate(
        ISchemaMergeContext context,
        IReadOnlyList<ITypeInfo> types);
}
