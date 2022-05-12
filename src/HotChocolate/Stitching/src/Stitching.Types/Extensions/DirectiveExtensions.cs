using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Extensions;

public static class DirectiveExtensions
{
    public static IReadOnlyList<DirectiveNode> ReplaceOrAddDirective(
        this IReadOnlyList<DirectiveNode> sourceDirectives,
        DirectiveNode find,
        DirectiveNode replace)
    {
        var directives = new List<DirectiveNode>(sourceDirectives);
        var matchingDirective = directives.FindIndex(node => SyntaxComparer.BySyntax.Equals(node, find));
        if (matchingDirective >= 0)
        {
            directives[matchingDirective] = replace;
        }
        else
        {
            directives.Add(replace);
        }

        return directives;
    }
}
