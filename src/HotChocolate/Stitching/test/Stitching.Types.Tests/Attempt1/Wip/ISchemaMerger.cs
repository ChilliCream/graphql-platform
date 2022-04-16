using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface ISchemaMerger
{
    ISchemaDocument Merge(IEnumerable<ISchemaDocument> schemas);
}