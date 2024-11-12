using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class DirectiveTools
{
    public static IImmutableList<DirectiveNode> GetUserDirectives(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        if(directiveNodes.Count == 0)
        {
            return ImmutableArray<DirectiveNode>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<DirectiveNode>();

        foreach (var directiveNode in directiveNodes)
        {
            if (RequiredDirectiveParser.CanParse(directiveNode)
                || FieldDirectiveParser.CanParse(directiveNode)
                || TypeDirectiveParser.CanParse(directiveNode)
                || LookupDirectiveParser.CanParse(directiveNode))
            {
                continue;
            }

            builder.Add(directiveNode);
        }

        return builder.ToImmutable();
    }
}
