using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface IMergeOperationsProvider
{
    ICollection<IMergeSchemaNodeOperation> GetOperations(ISyntaxNode source);
}
