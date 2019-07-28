using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    public delegate void MergeDirectiveRuleDelegate(
        ISchemaMergeContext context,
        IReadOnlyList<IDirectiveTypeInfo> types);
}
