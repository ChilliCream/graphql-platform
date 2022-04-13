using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal class FieldDefinition : ISchemaNode<FieldDefinitionNode>
{
    private readonly ITypeDefinition _typeDefinition;

    public FieldDefinition(ITypeDefinition typeDefinition, FieldDefinitionNode definition)
    {
        _typeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
        Definition = definition;
    }

    public string Name => Definition.Name.Value;

    public MemberKind Kind => MemberKind.Field;

    public bool IsExtension => false;

    public FieldDefinitionNode Definition { get; private set; }

    public void RewriteDefinition(FieldDefinitionNode node)
    {
        _typeDefinition.RewriteField(Definition, node);

        Definition = node;
    }
}
