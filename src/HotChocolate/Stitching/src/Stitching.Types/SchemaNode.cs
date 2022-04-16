using System;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

[DebuggerDisplay(@"{typeof(SchemaNode<TNode>).Name} {Coordinate}")]
internal class SchemaNode<TNode> : ISchemaNode<TNode>
    where TNode : ISyntaxNode
{
    public SchemaNode(CoordinateFactory coordinateFactory, TNode node)
        : this(coordinateFactory, default, node)
    {
        Definition = node;
    }

    public SchemaNode(CoordinateFactory coordinateFactory, ISchemaNode? parent, TNode node)
    {
        Parent = parent;
        Definition = node;
        Coordinate = coordinateFactory.Invoke(this);
    }

    public TNode Definition { get; private set; }

    public ISchemaNode? Parent { get; }

    public ISchemaCoordinate2? Coordinate { get; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        if (replacement is TNode typedReplacement)
        {
            return RewriteDefinition(typedReplacement);
        }

        return this;
    }

    public ISchemaNode RewriteDefinition(TNode node)
    {
        Definition = node;

        return this;
    }
}
