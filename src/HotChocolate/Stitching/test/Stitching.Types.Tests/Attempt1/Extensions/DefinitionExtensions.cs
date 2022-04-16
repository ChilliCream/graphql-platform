using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1;

internal static class DefinitionExtensions
{
    public static T? GetParent<T>(this ISchemaNode node)
    {
        return (T?)node.Parent;
    }

    public static IEnumerable<ISchemaNode> GetAncestors(this ISchemaNode node)
    {
        while (node?.Parent is not null)
        {
            yield return node.Parent;
            node = node.Parent;
        }
    }
}
