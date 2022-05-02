using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal static class OperationsExtensions
{
    public static void Apply(
        this IEnumerable<IMergeSchemaNodeOperation> operations,
        ISchemaNode source,
        ISchemaNode target,
        MergeOperationContext context)
    {
        foreach (IMergeSchemaNodeOperation operation in operations)
        {
            operation.Apply(source, target, context);
            target.Database.Reindex(target.Parent ?? target);
        }
    }

    public static TDefinition Apply<TDefinition>(
        this IEnumerable<IMergeSchemaNodeOperation> operations,
        TDefinition source,
        TDefinition target,
        MergeOperationContext context)
        where TDefinition : ISchemaNode
    {
        foreach (IMergeSchemaNodeOperation operation in operations)
        {
            operation.Apply(source, target, context);
        }

        return target;
    }
}
