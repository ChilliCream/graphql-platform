using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class InterfaceMergeHelper
{
    public static TDefinition MergeInterfaces<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : ComplexTypeDefinitionNodeBase, IHasWithInterfaces<TDefinition>, ISyntaxNode
    {
        IReadOnlyList<NamedTypeNode> interfaces = target.Interfaces
            .Concat(source.Interfaces)
            .Distinct()
            .ToList();

        return target.WithInterfaces(interfaces);
    }

    public static TTargetDefinition MergeInterfaces<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : ComplexTypeDefinitionNodeBase, ISyntaxNode
        where TTargetDefinition : ComplexTypeDefinitionNodeBase, IHasWithInterfaces<TTargetDefinition>, ISyntaxNode
    {
        IReadOnlyList<NamedTypeNode> interfaces = target.Interfaces
            .Concat(source.Interfaces)
            .Distinct()
            .ToList();

        return target.WithInterfaces(interfaces);
    }
}
