using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal sealed class ObjectTypeDefinition : ITypeDefinition<ObjectTypeDefinitionNode>
{
    private readonly DocumentDefinition _parentDefinition;

    public ObjectTypeDefinition(
        ISchemaDatabase database,
        ISchemaCoordinate2 coordinate,
        DocumentDefinition parentDefinition,
        ObjectTypeDefinitionNode definition)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        Coordinate = coordinate;
        _parentDefinition = parentDefinition ?? throw new ArgumentNullException(nameof(parentDefinition));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public NameNode Name => Definition.Name;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ISchemaDatabase Database { get; }

    public ObjectTypeDefinitionNode Definition { get; set; }

    public ISchemaNode Parent => _parentDefinition;

    public ISchemaCoordinate2? Coordinate { get; private set; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        switch (replacement)
        {
            case ObjectTypeDefinitionNode objectTypeDefinitionNode:
                return RewriteDefinition(objectTypeDefinitionNode);
            case { } when this.IsMemberOfInterface(original):
                return ReplaceInterface(original.Definition, replacement);
        }

        return this;
    }

    public ISchemaNode RewriteDefinition(ObjectTypeDefinitionNode node)
    {
        _parentDefinition.RewriteDefinition(Definition, node);

        Definition = node;
        Coordinate = Database.CalculateCoordinate(
            Coordinate?.Parent,
            Definition);

        return this;
    }

    public ISchemaNode RewriteField(FieldDefinitionNode original, FieldDefinitionNode replacement)
    {
        IReadOnlyList<FieldDefinitionNode> updatedFields = Definition.Fields
            .AddOrReplace(replacement, x => x.Equals(original));

        ObjectTypeDefinitionNode definition = Definition.WithFields(updatedFields);
        RewriteDefinition(definition);

        return this;
    }

    public ObjectTypeDefinition ReplaceInterface(ISyntaxNode original, ISyntaxNode replacement)
    {
        if (original is not NamedTypeNode originalNamedTypeNode || replacement is not NamedTypeNode namedTypeNode)
        {
            throw new InvalidOperationException($"Must be a {nameof(NamedTypeNode)}");
        }

        IReadOnlyList<NamedTypeNode> updatedInterfaces = Definition.Interfaces
            .AddOrReplace(namedTypeNode,
                x => x.Equals(originalNamedTypeNode));

        ObjectTypeDefinitionNode definition = Definition.WithInterfaces(updatedInterfaces);
        RewriteDefinition(definition);
        return this;
    }
}
