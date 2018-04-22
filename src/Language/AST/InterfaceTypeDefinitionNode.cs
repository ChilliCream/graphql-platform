using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InterfaceTypeDefinitionNode
        : ITypeDefinitionNode
    {
        public InterfaceTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
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

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Location = location;
            Name = name;
            Description = description;
            Directives = directives;
            Fields = fields;
        }

        public NodeKind Kind { get; } = NodeKind.InterfaceTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}