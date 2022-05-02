using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

[DebuggerDisplay(@"{typeof(SchemaNode<TNode>).Name} {Coordinate}")]
internal class SchemaNode<TNode> : ISchemaNode<TNode>
    where TNode : ISyntaxNode
{
    public SchemaNode(ISchemaDatabase database,
        ISchemaCoordinate2 coordinate,
        TNode node)
        : this(database, coordinate, default, node)
    {
        Definition = node;
    }

    public SchemaNode(ISchemaDatabase database, ISchemaCoordinate2 coordinate, ISchemaNode? parent, TNode node)
    {
        Parent = parent;
        Definition = node;
        Database = database;
        Coordinate = coordinate;
    }

    public ISchemaDatabase Database { get; }

    public TNode Definition { get; private set; }

    public ISchemaNode? Parent { get; }

    public ISchemaCoordinate2? Coordinate { get; private set; }

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
        Coordinate = Database.CalculateCoordinate(
            Coordinate?.Parent,
            Definition);

        return this;
    }
}
