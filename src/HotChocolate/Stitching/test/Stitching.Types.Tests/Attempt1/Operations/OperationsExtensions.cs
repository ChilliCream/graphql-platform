using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal static class OperationsExtensions
{
    public static void Apply<TDefinition>(
        this IEnumerable<ISchemaNodeOperation> operations,
        ISyntaxNode source,
        ISchemaNode<TDefinition> target,
        OperationContext context)
        where TDefinition : ISyntaxNode
    {
        foreach (ISchemaNodeOperation operation in operations)
        {
            target.Definition = (TDefinition)operation.Apply(source, target.Definition, context);
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