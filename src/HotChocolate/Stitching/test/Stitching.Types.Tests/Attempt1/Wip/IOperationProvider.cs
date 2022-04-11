using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface IOperationProvider
{
    ICollection<ISchemaNodeOperation> GetOperations(ISyntaxNode source);
}