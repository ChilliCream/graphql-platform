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

    public static SchemaCoordinate ToSchemaCoordinate(this IReadOnlyList<ISyntaxNode> syntaxNodes)
    {
        return CreateCoordinate(syntaxNodes);
    }

    private static SchemaCoordinate CreateCoordinate(IReadOnlyList<ISyntaxNode> nodes)
    {
        IReadOnlyList<NameNode> namedSyntaxNodes = GetCoordinateNames(nodes);

        switch (namedSyntaxNodes.Count)
        {
            case 1:
                return new SchemaCoordinate(new NameString(namedSyntaxNodes[0].Value));

            case 2:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
            case 3:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[2].Value),
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
        }

        return new SchemaCoordinate();
    }

    private static IReadOnlyList<NameNode> GetCoordinateNames(IReadOnlyList<ISyntaxNode> nodes)
    {
        var namedSyntaxNodes = new List<NameNode>();
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] is not INamedSyntaxNode namedSyntaxNode)
            {
                continue;
            }

            namedSyntaxNodes.Add(namedSyntaxNode.Name);
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
