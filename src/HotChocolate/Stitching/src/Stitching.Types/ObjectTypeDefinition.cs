using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal sealed class ObjectTypeDefinition : ITypeDefinition<ObjectTypeDefinitionNode>
{
    private readonly DocumentDefinition _parentDefinition;

    public ObjectTypeDefinition(Func<ISchemaNode, ISchemaCoordinate2> coordinateFactory, DocumentDefinition parentDefinition, ObjectTypeDefinitionNode definition)
    {
        _parentDefinition = parentDefinition ?? throw new ArgumentNullException(nameof(parentDefinition));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Coordinate = coordinateFactory.Invoke(this);
    }

    public string Name => Definition.Name.Value;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ObjectTypeDefinitionNode Definition { get; set; }

    public ISchemaNode? Parent => _parentDefinition;
    public ISchemaCoordinate2 Coordinate { get; }

    public void RewriteDefinition(ObjectTypeDefinitionNode node)
    {
        _parentDefinition.RewriteDefinition(Definition, node);

        Definition = node;
    }

    public void RewriteField(FieldDefinitionNode original, FieldDefinitionNode replacement)
    {
        IReadOnlyList<FieldDefinitionNode> updatedFields = Definition.Fields
            .AddOrReplace(replacement, x => x.Name.Equals(original.Name));

        ObjectTypeDefinitionNode definition = Definition.WithFields(updatedFields);
        RewriteDefinition(definition);
    }
}
