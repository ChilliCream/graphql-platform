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
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> interfaces,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives)
        {
            if (interfaces == null)
            {
                throw new ArgumentNullException(nameof(interfaces));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Interfaces = interfaces;
            Fields = fields;
        }

        public IReadOnlyCollection<NamedTypeNode> Interfaces { get; }

        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}
