using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class ObjectTypeDefinitionNodeBase
        : ComplexTypeDefinitionNodeBase
    {
        protected ObjectTypeDefinitionNodeBase(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> interfaces,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        {
        }
    }
}
