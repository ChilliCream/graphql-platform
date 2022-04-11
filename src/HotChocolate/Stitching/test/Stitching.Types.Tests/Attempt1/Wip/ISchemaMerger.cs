using System.Collections.Generic;

namespace HotChocolate.Stitching.Types;

public interface ISchemaMerger
{
    ISchemaDocument Merge(IEnumerable<ISchemaDocument> schemas);
}