using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

internal sealed class InterfaceTypeDefinition : ITypeDefinition<InterfaceTypeDefinitionNode>
{
    private readonly DocumentDefinition _parentDefinition;

    public InterfaceTypeDefinition(DocumentDefinition parentDefinition, InterfaceTypeDefinitionNode definition)
    {
        _parentDefinition = parentDefinition ?? throw new ArgumentNullException(nameof(parentDefinition));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public string Name => Definition.Name.Value;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public InterfaceTypeDefinitionNode Definition { get; set; }

    public void RewriteDefinition(InterfaceTypeDefinitionNode node)
    {
        _parentDefinition.RewriteDefinition(Definition, node);

        Definition = node;
    }

    public void RewriteField(FieldDefinitionNode original, FieldDefinitionNode replacement)
    {
        IReadOnlyList<FieldDefinitionNode> updatedFields = Definition.Fields
            .AddOrReplace(replacement, x => x.Name.Equals(original.Name));

        RewriteDefinition(Definition.WithFields(updatedFields));
    }
}
