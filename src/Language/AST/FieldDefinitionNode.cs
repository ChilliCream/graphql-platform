using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FieldDefinitionNode
        : NamedSyntaxNode
    {
        public FieldDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<InputValueDefinitionNode> arguments,
            ITypeNode type,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Description = description;
            Arguments = arguments;
            Type = type;
        }

        public override NodeKind Kind { get; } = NodeKind.FieldDefinition;

        public StringValueNode Description { get; }

        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }

        public ITypeNode Type { get; }
    }
}
