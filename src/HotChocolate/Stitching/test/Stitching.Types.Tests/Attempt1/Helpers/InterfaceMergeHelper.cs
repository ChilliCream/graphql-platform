using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Helpers;

internal static class InterfaceMergeHelper
{
    public static TDefinition MergeInterfaces<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target,
        Func<IReadOnlyList<NamedTypeNode>, TDefinition> action)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : ComplexTypeDefinitionNodeBase, ISyntaxNode
    {
        IEnumerable<NamedTypeNode> interfaces = target.Interfaces
            .Concat(source.Interfaces)
            .Distinct();

        return action.Invoke(interfaces.ToList());
    }

    public static TTargetDefinition MergeInterfaces<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target,
        Func<IReadOnlyList<NamedTypeNode>, TTargetDefinition> action)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : ComplexTypeDefinitionNodeBase, ISyntaxNode
        where TTargetDefinition : ComplexTypeDefinitionNodeBase, ISyntaxNode
    {
        IEnumerable<NamedTypeNode> interfaces = target.Interfaces
            .Concat(source.Interfaces)
            .Distinct();

        return action.Invoke(interfaces.ToList());
    }
}