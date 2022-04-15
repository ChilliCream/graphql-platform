using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal static class NodeExtensions
{
    public static IEnumerable<ISchemaNode> DescendentNodes(this ISchemaNode root, ISchemaCoordinate2Provider coordinate2Provider)
    {
        coordinate2Provider.TryGet(root, out ISchemaCoordinate2? coordinate);
        coordinate2Provider.TryGet(coordinate, out ISchemaNode? schemaNode);
        var nodes = new Stack<ISchemaNode>();
        nodes.Push(schemaNode!);
        while (nodes.Any())
        {
            ISchemaNode node = nodes.Pop();
            yield return node;

            if (node.Definition is ObjectTypeDefinitionNode objectTypeDefinition && objectTypeDefinition.Name.Value == "Test")
            {

            }

            foreach (ISyntaxNode child in node.Definition.GetNodes())
            {
                coordinate2Provider.TryGet(node, out ISchemaCoordinate2? parentCoordinate);
                coordinate2Provider.TryGet(parentCoordinate, out ISchemaNode? parentSchemaNode);
                ISchemaCoordinate2 childCoordinate = coordinate2Provider.CalculateCoordinate(parentSchemaNode, child);
                coordinate2Provider.TryGet(childCoordinate, out ISchemaNode? childSchemaNode);

                if (childSchemaNode is null)
                {
                    continue;
                }

                nodes.Push(childSchemaNode);
            }
        }
    }
}
