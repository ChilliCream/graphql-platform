using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class EnumTypeDefinitionNode
        : ITypeDefinitionNode
        , IHasDirectives
    {
        public EnumTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Location = location;
            Name = name;
            Description = description;
            Directives = directives;
            Values = values;
        }

        public NodeKind Kind { get; } = NodeKind.EnumTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}
