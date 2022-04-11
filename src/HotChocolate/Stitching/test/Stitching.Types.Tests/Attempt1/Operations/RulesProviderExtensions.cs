using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal static class RulesProviderExtensions
{
    public static void Apply<TDefinition>(
        this ISchemaNode<TDefinition> target,
        ISyntaxNode source,
        IOperationProvider operationProvider)
        where TDefinition : ISyntaxNode
    {
        ICollection<ISchemaNodeOperation> operations = operationProvider.GetOperations(source);
        var context = new OperationContext(operationProvider);
        operations.Apply(source, target, context);
    }
}