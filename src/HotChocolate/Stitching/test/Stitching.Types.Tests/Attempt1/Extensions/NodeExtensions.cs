using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Traversal;

namespace HotChocolate.Stitching.Types.Attempt1;

internal static class NodeExtensions
{
    public static IEnumerable<ISchemaNode> DescendentNodes(this ISchemaNode root)
    {
        var database = root.Database;
        var nodes = DescendentSyntaxNodes(root)
            .ToList();

        foreach (SyntaxNodeReference node in nodes)
        {
            ISchemaNode schemaNode = database.GetOrAdd(node.Coordinate, node.Node);
            yield return schemaNode;
        }
    }

    public static IEnumerable<SyntaxNodeReference> ChildSyntaxNodes(this ISchemaNode node)
    {
        var coordinate = node.Database.CalculateCoordinate(node.Parent?.Coordinate, node.Definition);
        var rootReference = new SyntaxNodeReference(default, coordinate, node.Definition);
        return ChildSyntaxNodes(rootReference,
            node.Definition,
            node.Database);
    }

    public static IEnumerable<SyntaxNodeReference> DescendentSyntaxNodes(this ISchemaNode root)
    {
        var nodes = new Stack<SyntaxNodeReference>();
        var coordinate = root.Database.CalculateCoordinate(root.Parent?.Coordinate, root.Definition);
        var rootReference = new SyntaxNodeReference(default, coordinate, root.Definition);
        nodes.Push(rootReference);

        while (nodes.Count != 0)
        {
            SyntaxNodeReference node = nodes.Pop();

            yield return node;

            IEnumerable<SyntaxNodeReference> children = ChildSyntaxNodes(
                node,
                node.Node,
                root.Database);

            foreach (SyntaxNodeReference child in children)
            {
                nodes.Push(child);

                yield return child;
            }
        }
    }

    private static IEnumerable<SyntaxNodeReference> ChildSyntaxNodes(
        SyntaxNodeReference? parentReference,
        ISyntaxNode node,
        ISchemaDatabase database)
    {
        IEnumerable<ISyntaxNode> children = node.GetNodes();
        foreach (ISyntaxNode child in children)
        {
            var childCoordinate = database.CalculateCoordinate(parentReference?.Coordinate, child);
            yield return new SyntaxNodeReference(parentReference, childCoordinate, child);
        }
    }
}

