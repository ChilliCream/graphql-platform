using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class DirectiveMergeHelper
{
    public static void MergeDirectivesInto<TSchemaNode>(
        this TSchemaNode source,
        TSchemaNode target)
        where TSchemaNode : ISchemaNode
    {
        if (source.Definition is not IHasDirectives sourceWithDirectives
            || target.Definition is not (IHasDirectives targetWithDirectives
                and IHasWithDirectives<ISyntaxNode> hasWithDirectives))
        {
            return;
        }

        IReadOnlyList<DirectiveNode> directives = targetWithDirectives.Directives
            .Concat(sourceWithDirectives.Directives.PatchWithSchema(source.Database))
            .Distinct()
            .ToList();

        target.RewriteDefinition(hasWithDirectives.WithDirectives(directives));
    }
}
