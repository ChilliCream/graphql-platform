using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Helpers;

internal static class DirectiveMergeHelper
{
    public static TDefinition MergeDirectives<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target,
        Func<IReadOnlyList<DirectiveNode>, TDefinition> action)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : IHasDirectives, ISyntaxNode
    {
        IEnumerable<DirectiveNode> directives = target.Directives
            .Concat(source.Directives)
            .Distinct();

        return action.Invoke(directives.ToList());
    }

    public static TTargetDefinition MergeDirectives<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target,
        Func<IReadOnlyList<DirectiveNode>, TTargetDefinition> action)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : IHasDirectives, ISyntaxNode
        where TTargetDefinition : IHasDirectives, ISyntaxNode
    {
        IEnumerable<DirectiveNode> directives = target.Directives
            .Concat(source.Directives)
            .Distinct();

        return action.Invoke(directives.ToList());
    }

}
