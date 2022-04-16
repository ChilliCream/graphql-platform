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
        var nodes = new Stack<ISyntaxNode>();
        nodes.Push(root.Definition);

        SyntaxNodeReference? parentReference = default;

        while (nodes.Any())
        {
            ISyntaxNode node = nodes.Pop();

            parentReference = new SyntaxNodeReference(parentReference, node);
            yield return (SyntaxNodeReference) parentReference;

            IEnumerable<ISyntaxNode> children = node.GetNodes();
            foreach (ISyntaxNode child in children)
            {
                yield return new SyntaxNodeReference(parentReference, child);

                nodes.Push(child);
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

