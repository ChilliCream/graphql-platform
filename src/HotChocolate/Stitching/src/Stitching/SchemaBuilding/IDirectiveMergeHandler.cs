using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface IDirectiveMergeHandler
{
    void Merge(
        ISchemaMergeContext context,
        IReadOnlyList<IDirectiveTypeInfo> directives);
}
