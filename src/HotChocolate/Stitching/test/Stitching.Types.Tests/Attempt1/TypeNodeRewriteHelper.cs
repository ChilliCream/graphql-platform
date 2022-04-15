using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class TypeNodeRewriteHelper
{
    public static ISchemaNodeInfo<ITypeNode> Rewrite<T>(ISchemaNode? schemaNode, T node)
        where T : ISyntaxNode
    {
        if (schemaNode is null)
        {
            throw new ArgumentNullException(nameof(schemaNode));
        }

        Func<ISchemaNode, ISchemaCoordinate2> coordinateFactory = schemaNode.Coordinate.Provider.Add;
        ISchemaNode? currentNode = new SchemaNode<T>(coordinateFactory, schemaNode.Parent, node);

        while (currentNode.Parent?.Definition is ITypeNode parentTypeNode)
        {
            switch (parentTypeNode)
            {
                case NonNullTypeNode when currentNode.Definition is INullableTypeNode nullableTypeNode:
                    NonNullTypeNode newNonNullTypeNode = new NonNullTypeNode(nullableTypeNode);
                    currentNode.Parent.RewriteDefinition(newNonNullTypeNode);
                    break;
                case ListTypeNode when currentNode.Parent.Definition is ITypeNode typeNode:
                    ListTypeNode newListTypeNode = new ListTypeNode(typeNode);
                    currentNode.Parent.RewriteDefinition(newListTypeNode);
                    break;
                default:
                    throw new NotImplementedException();
            }

            currentNode = currentNode.Parent;
        }

        return (ISchemaNodeInfo<ITypeNode>)currentNode;
    }
}
