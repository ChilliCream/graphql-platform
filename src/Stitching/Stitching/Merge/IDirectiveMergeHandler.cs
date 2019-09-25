using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    public interface IDirectiveMergeHandler
    {
        void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<IDirectiveTypeInfo> directives);
    }
}
