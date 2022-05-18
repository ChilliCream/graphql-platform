using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public static class SchemaCoordinateExtensions
{
    public static IReadOnlyList<string> GetNames(this IReadOnlyList<ISyntaxNode> nodes)
    {
        var namedSyntaxNodes = new List<string>();
        for (var i = 0; i < nodes.Count; i++)
        {
            ISyntaxNode node = nodes[i];
            NameNode? name = GetName(node);
            if (name is null)
            {
                continue;
            }

            namedSyntaxNodes.Add(name.Value);
        }

        return namedSyntaxNodes;
    }

    private static NameNode? GetName(ISyntaxNode node)
    {
        switch (node)
        {
            case INamedSyntaxNode namedSyntaxNode:
                return namedSyntaxNode.Name;

            case NamedTypeNode namedTypeNode:
                return namedTypeNode.Name;

            case DirectiveNode directiveNode:
                return directiveNode.Name;
        }

        return default;
    }
}
