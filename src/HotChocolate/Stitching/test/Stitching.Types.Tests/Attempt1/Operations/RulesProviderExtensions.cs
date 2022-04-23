using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal static class RulesProviderExtensions
{
    public static void Apply(
        this ISchemaNode target,
        ISyntaxNode source,
        MergeOperationContext context)
    {
        ICollection<IMergeSchemaNodeOperation> operations = context
            .OperationProvider
            .GetOperations(source);

        operations.Apply(source, target, context);
    }
}
