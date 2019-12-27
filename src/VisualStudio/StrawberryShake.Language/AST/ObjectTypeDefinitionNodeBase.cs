using System;
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
            : base(location, name, directives, fields)
        {
            Interfaces = interfaces
                ?? throw new ArgumentNullException(nameof(interfaces));
        }

        public IReadOnlyList<NamedTypeNode> Interfaces { get; }
    }
}
