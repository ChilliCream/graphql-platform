using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class DirectiveMergeHelper
{
    public static void MergeDirectivesInto<TSchemaNode>(
        this TSchemaNode source,
        TSchemaNode target,
        MergeOperationContext context)
        where TSchemaNode : ISchemaNode
    {
        MergeDirectivesInto(source.Definition, target, context);
    }

    public static void MergeDirectivesInto<TSchemaNode>(
        this ISyntaxNode source,
        TSchemaNode target,
        MergeOperationContext context)
        where TSchemaNode : ISchemaNode
    {

        if (source is not IHasDirectives sourceWithDirectives
            || target.Definition is not (IHasDirectives targetWithDirectives
                and IHasWithDirectives<ISyntaxNode> hasWithDirectives))
        {
            return;
        }

        IReadOnlyList<DirectiveNode> directives = targetWithDirectives.Directives
            .Concat(sourceWithDirectives.Directives.PatchWithSchema(context.Source))
            .Distinct()
            .ToList();

        target.RewriteDefinition(hasWithDirectives.WithDirectives(directives));
    }
}
