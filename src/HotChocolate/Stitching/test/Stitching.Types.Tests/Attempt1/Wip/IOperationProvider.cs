using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface IOperationProvider
{
    ICollection<ISchemaNodeOperation> GetOperations(ISyntaxNode source);
}
