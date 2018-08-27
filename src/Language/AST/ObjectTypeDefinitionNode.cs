using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ObjectTypeDefinitionNode
        : ITypeDefinitionNode
        , IHasDirectives
    {
        public ObjectTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> interfaces,
            IReadOnlyCollection<FieldDefinitionNode> fields)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (interfaces == null)
            {
                throw new ArgumentNullException(nameof(interfaces));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Location = location;
            Name = name;
            Description = description;
            Directives = directives;
            Interfaces = interfaces;
            Fields = fields;
        }

        public NodeKind Kind { get; } = NodeKind.ObjectTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Interfaces { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}
