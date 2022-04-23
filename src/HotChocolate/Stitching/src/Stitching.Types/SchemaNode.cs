using System;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

[DebuggerDisplay(@"{typeof(SchemaNode<TNode>).Name} {Coordinate}")]
internal class SchemaNode<TNode> : ISchemaNode<TNode>
    where TNode : ISyntaxNode
{
    public SchemaNode(ISchemaCoordinate2 coordinate,
        TNode node)
        : this(default, coordinate, node)
    {
        Definition = node;
    }

    public SchemaNode(ISchemaNode? parent, ISchemaCoordinate2 coordinate, TNode node)
    {
        Parent = parent;
        Definition = node;
        Coordinate = coordinate;
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
