using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class InterfaceMergeHelper
{
    public static void MergeInterfacesInto<TSchemaNode>(
        this TSchemaNode source,
        TSchemaNode target,
        MergeOperationContext context)
        where TSchemaNode : ISchemaNode
    {
        MergeInterfacesInto(source.Definition, target, context);
    }

    public static void MergeInterfacesInto<TSchemaNode>(this ISyntaxNode source,
        TSchemaNode target,
        MergeOperationContext _)
        where TSchemaNode : ISchemaNode
    {
        if (source is not IHasInterfaces sourceWithInterfaces
            || target.Definition is not (IHasInterfaces targetWithInterfaces
                and IHasWithInterfaces<ISyntaxNode> hasWithInterfaces))
            return;

        IReadOnlyList<NamedTypeNode> interfaces = targetWithInterfaces.Interfaces
            .Concat(sourceWithInterfaces.Interfaces)
            .Distinct()
            .ToList();

        target.RewriteDefinition(hasWithInterfaces.WithInterfaces(interfaces));
    }
}
