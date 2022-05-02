using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class InterfaceMergeHelper
{
    public static void MergeInterfacesInto<TSchemaNode>(
        this TSchemaNode source,
        TSchemaNode target)
        where TSchemaNode : ISchemaNode
    {
        MergeInterfacesInto(source.Definition, target);
    }

    public static void MergeInterfacesInto<TSchemaNode>(
        this ISyntaxNode source,
        TSchemaNode target)
        where TSchemaNode : ISchemaNode
    {
        if (source is not IHasInterfaces sourceWithInterfaces
            || target.Definition is not (IHasInterfaces targetWithInterfaces
                and IHasWithInterfaces<ISyntaxNode> hasWithInterfaces))
        {
            return;
        }

        IReadOnlyList<NamedTypeNode> interfaces = targetWithInterfaces.Interfaces
            .Concat(sourceWithInterfaces.Interfaces)
            .Distinct()
            .ToList();

        target.RewriteDefinition(hasWithInterfaces.WithInterfaces(interfaces));
    }
}
