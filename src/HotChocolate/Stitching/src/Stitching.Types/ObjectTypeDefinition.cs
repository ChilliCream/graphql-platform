using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal sealed class ObjectTypeDefinition : ITypeDefinition<ObjectTypeDefinitionNode>
{
    private readonly DocumentDefinition _parentDefinition;

    public ObjectTypeDefinition(CoordinateFactory coordinateFactory, DocumentDefinition parentDefinition, ObjectTypeDefinitionNode definition)
    {
        _parentDefinition = parentDefinition ?? throw new ArgumentNullException(nameof(parentDefinition));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Coordinate = coordinateFactory.Invoke(this);
    }

    public NameNode Name => Definition.Name;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ObjectTypeDefinitionNode Definition { get; set; }

    public ISchemaNode? Parent => _parentDefinition;

    public ISchemaCoordinate2? Coordinate { get; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        switch (replacement)
        {
            case ObjectTypeDefinitionNode objectTypeDefinitionNode:
                return RewriteDefinition(objectTypeDefinitionNode);
            case { } when this.IsInterfaceNode(original):
                return ReplaceInterface(original.Definition, replacement);
        }

        return this;
    }

    public ISchemaNode RewriteDefinition(ObjectTypeDefinitionNode node)
    {
        _parentDefinition.RewriteDefinition(Definition, node);

        Definition = node;

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
