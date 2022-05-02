using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal sealed class InterfaceTypeDefinition : ITypeDefinition<InterfaceTypeDefinitionNode>
{
    private readonly DocumentDefinition _parentDefinition;

    public InterfaceTypeDefinition(
        ISchemaDatabase database,
        ISchemaCoordinate2 coordinate,
        DocumentDefinition parentDefinition,
        InterfaceTypeDefinitionNode definition)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _parentDefinition = parentDefinition ?? throw new ArgumentNullException(nameof(parentDefinition));
        Coordinate = coordinate;
    }

    public NameNode Name => Definition.Name;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ISchemaDatabase Database { get; }

    public InterfaceTypeDefinitionNode Definition { get; set; }

    public ISchemaNode? Parent => _parentDefinition;

    public ISchemaCoordinate2? Coordinate { get; private set; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        switch (replacement)
        {
            case InterfaceTypeDefinitionNode interfaceTypeDefinitionNode:
                return RewriteDefinition(interfaceTypeDefinitionNode);
        }

        return this;
    }

    public ISchemaNode RewriteDefinition(InterfaceTypeDefinitionNode node)
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
            .AddOrReplace(replacement, x => x.Name.Equals(original.Name));

        RewriteDefinition(Definition.WithFields(updatedFields));

        return this;
    }
}
