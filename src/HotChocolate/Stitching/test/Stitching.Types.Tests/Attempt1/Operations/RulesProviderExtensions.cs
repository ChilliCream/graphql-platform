using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal static class RulesProviderExtensions
{
    public static void Apply(
        this ISchemaNode target,
        ISyntaxNode source,
        IOperationProvider operationProvider)
    {
        ICollection<ISchemaNodeOperation> operations = operationProvider.GetOperations(source);
        var context = new OperationContext(operationProvider);
        operations.Apply(source, target, context);
    }
}
