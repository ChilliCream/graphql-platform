using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

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
