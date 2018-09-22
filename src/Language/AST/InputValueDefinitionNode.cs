using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InputValueDefinitionNode
        : NamedSyntaxNode
    {
        public InputValueDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            ITypeNode type,
            IValueNode defaultValue,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Description = description;
            Type = type;
            DefaultValue = defaultValue;
        }

        public override NodeKind Kind { get; } = NodeKind.InputValueDefinition;

        public StringValueNode Description { get; }

        public ITypeNode Type { get; }

        public IValueNode DefaultValue { get; }
    }
}
