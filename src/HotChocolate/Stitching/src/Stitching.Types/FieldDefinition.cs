using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal class FieldDefinition : ISchemaNode<FieldDefinitionNode>
{
    private readonly ITypeDefinition _typeDefinition;

    public FieldDefinition(
        ISchemaCoordinate2 coordinate,
        ITypeDefinition typeDefinition,
        FieldDefinitionNode definition)
    {
        _typeDefinition = typeDefinition
                          ?? throw new ArgumentNullException(nameof(typeDefinition));

        Definition = definition;
        Coordinate = coordinate;
    }

    public NameNode Name => Definition.Name;

    public MemberKind Kind => MemberKind.Field;

    public bool IsExtension => false;

    public FieldDefinitionNode Definition { get; private set; }

    public ISchemaNode Parent => _typeDefinition;

    public ISchemaCoordinate2 Coordinate { get; }

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

        return this;
    }
}
