using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class SchemaNode<TNode> : ISchemaNode<TNode>
    where TNode : ISyntaxNode
{
    public TNode Definition { get; private set; }
    public ISchemaNode? Parent { get; }
    public ISchemaCoordinate2 Coordinate { get; }

    public SchemaNode(Func<ISchemaNode, ISchemaCoordinate2> coordinateFactory, TNode node)
        : this(coordinateFactory, default, node)
    {
        Definition = node;
    }

    public SchemaNode(Func<ISchemaNode, ISchemaCoordinate2> coordinateFactory, ISchemaNode? parent, TNode node)
    {
        Parent = parent;
        Definition = node;
        Coordinate = coordinateFactory.Invoke(this);
    }

    public void RewriteDefinition(TNode node)
    {
        Definition = node;
    }
}
