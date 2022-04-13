using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal static class OperationsExtensions
{
    public static void Apply(
        this IEnumerable<ISchemaNodeOperation> operations,
        ISyntaxNode source,
        ISchemaNode target,
        OperationContext context)
    {
        foreach (ISchemaNodeOperation operation in operations)
        {
            ISyntaxNode definition = operation.Apply(source, target.Definition, context);
            target.RewriteDefinition(definition);
        }
    }

    public static TDefinition Apply<TDefinition>(
        this IEnumerable<ISchemaNodeOperation> operations,
        TDefinition source,
        TDefinition target,
        OperationContext context)
        where TDefinition : ISyntaxNode
    {
        foreach (ISchemaNodeOperation operation in operations)
        {
            target = (TDefinition) operation.Apply(source, target, context);
        }

        return target;
    }
}
