using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

internal class ApplyObjectTypeExtension
    : ApplyComplexTypeExtension<ObjectTypeDefinitionNode, ObjectTypeExtensionNode>
{
    protected override ObjectTypeDefinitionNode CreateDefinition(
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        => new(null, name, description, directives, interfaces, fields);
}
