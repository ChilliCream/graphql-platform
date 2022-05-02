using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal class FieldDefinition : ISchemaNode<FieldDefinitionNode>
{
    private readonly ITypeDefinition _typeDefinition;

    public FieldDefinition(
        ISchemaDatabase database,
        ISchemaCoordinate2 coordinate,
        ITypeDefinition typeDefinition,
        FieldDefinitionNode definition)
    {
        Database = database;
        Coordinate = coordinate;
        _typeDefinition = typeDefinition
                          ?? throw new ArgumentNullException(nameof(typeDefinition));
        Definition = definition;
    }

    public NameNode Name => Definition.Name;

    public MemberKind Kind => MemberKind.Field;

    public bool IsExtension => false;

    public ISchemaDatabase Database { get; }

    public FieldDefinitionNode Definition { get; private set; }

    public ISchemaNode Parent => _typeDefinition;

    public ISchemaCoordinate2 Coordinate { get; private set; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        switch (replacement)
        {
            case FieldDefinitionNode fieldDefinitionNode:
                return RewriteDefinition(fieldDefinitionNode);
        }

        return this;
    }

    public ISchemaNode RewriteDefinition(FieldDefinitionNode node)
    {
        _typeDefinition.RewriteField(Definition, node);

        Definition = node;
        Coordinate = Database.CalculateCoordinate(
            Coordinate?.Parent,
            Definition);

        return this;
    }
}
