using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal static class OperationsExtensions
{
    public static void Apply(
        this IEnumerable<IMergeSchemaNodeOperation> operations,
        ISyntaxNode source,
        ISchemaNode target,
        MergeOperationContext context)
    {
        foreach (IMergeSchemaNodeOperation operation in operations)
        {
            ISyntaxNode definition = operation.Apply(source, target.Definition, context);
            target.RewriteDefinition(definition);
            context.Database.Reindex(target.Parent ?? target);
        }
    }

    public static TDefinition Apply<TDefinition>(
        this IEnumerable<IMergeSchemaNodeOperation> operations,
        TDefinition source,
        TDefinition target,
        MergeOperationContext context)
        where TDefinition : ISyntaxNode
    {
        foreach (IMergeSchemaNodeOperation operation in operations)
        {
            target = (TDefinition) operation.Apply(source, target, context);
        }

        return target;
    }
}
