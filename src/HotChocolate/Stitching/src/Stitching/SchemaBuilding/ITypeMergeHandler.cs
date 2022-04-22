using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface ITypeMergeHandler
{
    void Merge(
        ISchemaMergeContext context,
        IReadOnlyList<ITypeInfo> types);
}
