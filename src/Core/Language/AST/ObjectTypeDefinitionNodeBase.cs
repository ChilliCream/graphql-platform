using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class ObjectTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected ObjectTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> interfaces,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives)
        {
            Interfaces = interfaces
                ?? throw new ArgumentNullException(nameof(interfaces));
            Fields = fields
                ?? throw new ArgumentNullException(nameof(fields));
        }

        public IReadOnlyList<NamedTypeNode> Interfaces { get; }

        public IReadOnlyList<FieldDefinitionNode> Fields { get; }
    }
}
