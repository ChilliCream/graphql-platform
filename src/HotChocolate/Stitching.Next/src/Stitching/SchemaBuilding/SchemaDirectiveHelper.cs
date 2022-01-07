using System.Collections.Generic;
using HotChocolate.Language;
namespace HotChocolate.Stitching.SchemaBuilding;

internal static class SchemaDirectiveHelper
{
    public static IList<ISchemaBuildingDirective> ParseDirectives(IHasDirectives syntaxNode)
    {
        var directives = new List<ISchemaBuildingDirective>();

        foreach (DirectiveNode directive in syntaxNode.Directives)
        {
            if (IsDirective.TryParse(directive, out var isDirective))
            {
                directives.Add(isDirective);
                continue;
            }
        }

        return directives;
    }
}
