using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;

namespace HotChocolate.Stitching.Types.Attempt1;

internal static class NodeExtensions
{
    public static IEnumerable<ISchemaNode> DescendentNodes(this ISchemaNode root)
    {
        ISchemaDatabase database = root.Database;
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
        ISchemaCoordinate2 coordinate = node.Database.CalculateCoordinate(node.Parent?.Coordinate, node.Definition);
        var rootReference = new SyntaxNodeReference(default, coordinate, node.Definition);
        return ChildSyntaxNodes(rootReference,
            node.Definition,
            node.Database);
    }

    public static IEnumerable<SyntaxNodeReference> DescendentSyntaxNodes(this ISchemaNode root)
    {
        return DescendentSyntaxNodes(root.Definition, root.Coordinate, root.Database);
    }

    public static IEnumerable<SyntaxNodeReference> DescendentSyntaxNodes(this ISyntaxNode root)
    {
        var database = new SchemaDatabase();
        ISchemaCoordinate2 coordinate = database.CalculateCoordinate(default, root);

        return DescendentSyntaxNodes(root, coordinate, database);
    }

    private static IEnumerable<SyntaxNodeReference> DescendentSyntaxNodes(
        ISyntaxNode root,
        ISchemaCoordinate2 coordinate,
        ISchemaDatabase database)
    {

        var rootReference = new SyntaxNodeReference(default, coordinate, root);

        var nodes = new Stack<SyntaxNodeReference>();
        nodes.Push(rootReference);

        while (nodes.Count != 0)
        {
            SyntaxNodeReference node = nodes.Pop();

            yield return node;

            IEnumerable<SyntaxNodeReference> children = ChildSyntaxNodes(
                node,
                node.Node,
                database);

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
            ISchemaCoordinate2 childCoordinate = database.CalculateCoordinate(parentReference?.Coordinate, child);
            yield return new SyntaxNodeReference(parentReference, childCoordinate, child);
        }
    }
}

