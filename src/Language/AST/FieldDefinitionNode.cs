using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldDefinitionNode
        : ISyntaxNode
        , IHasDirectives
    {
        public FieldDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<InputValueDefinitionNode> arguments,
            ITypeNode type,
            IReadOnlyCollection<DirectiveNode> directives)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            Location = location;
            Name = name;
            Description = description;
            Arguments = arguments;
            Type = type;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.FieldDefinition;

        public Location Location { get; }

        public NameNode Name { get; }

        public StringValueNode Description { get; }

        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }

        public ITypeNode Type { get; }

        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}
