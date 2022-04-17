using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class DirectiveMergeHelper
{
    public static TDefinition MergeDirectives<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : IHasDirectives, IHasWithDirectives<TDefinition>, ISyntaxNode
    {
        IReadOnlyList<DirectiveNode> directives = target.Directives
            .Concat(source.Directives)
            .Distinct()
            .ToList();

        return target.WithDirectives(directives);
    }

    public static TTargetDefinition MergeDirectives<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : IHasDirectives, ISyntaxNode
        where TTargetDefinition : IHasDirectives, IHasWithDirectives<TTargetDefinition>, ISyntaxNode
    {
        IReadOnlyList<DirectiveNode> directives = target.Directives
            .Concat(source.Directives)
            .Distinct()
            .ToList();

        return target.WithDirectives(directives);
    }
}
