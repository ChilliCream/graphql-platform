using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Traversal;

namespace HotChocolate.Stitching.Types.Attempt1;

internal static class NodeExtensions
{
    public static IEnumerable<ISchemaNode> DescendentNodes(this ISchemaNode root, ISchemaDatabase database)
    {
        var nodes = DescendentSyntaxNodes(root)
            .ToList();

        foreach (SyntaxNodeReference node in nodes)
        {
            if (!database.TryGetExact(node.Node, out ISchemaNode? schemaNode))
            {
                continue;
            }

            yield return schemaNode;
        }
    }

    public static IEnumerable<SyntaxNodeReference> DescendentSyntaxNodes(this ISchemaNode root)
    {
        var nodes = new Stack<SyntaxNodeReference>();
        var rootReference = new SyntaxNodeReference(default, root.Definition);
        nodes.Push(rootReference);

        while (nodes.Any())
        {
            SyntaxNodeReference node = nodes.Pop();

            yield return node;

            IEnumerable<ISyntaxNode> children = node.Node.GetNodes();
            foreach (ISyntaxNode child in children)
            {
                var childReference = new SyntaxNodeReference(node, child);
                nodes.Push(childReference);

                yield return childReference;
            }
        }
    }

    public static IEnumerable<SyntaxNodeReference> ChildSyntaxNodes(this ISchemaNode node)
    {
        var parent = new SyntaxNodeReference(default, node.Definition);
        IEnumerable<ISyntaxNode> children = node.Definition.GetNodes();
        foreach (ISyntaxNode child in children)
        {
            yield return new SyntaxNodeReference(parent, child);
        }
    }
}

