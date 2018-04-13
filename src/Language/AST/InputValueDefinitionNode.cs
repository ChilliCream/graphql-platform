using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InputValueDefinitionNode
        : ISyntaxNode
    {
        public InputValueDefinitionNode(Location location, 
            NameNode name, StringValueNode description, 
            ITypeNode type, IValueNode defaultValue, 
            IReadOnlyCollection<DirectiveNode> directives)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
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
            Type = type;
            DefaultValue = defaultValue;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.InputValueDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public ITypeNode Type { get; }
        public IValueNode DefaultValue { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}