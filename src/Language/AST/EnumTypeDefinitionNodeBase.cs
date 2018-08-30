using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class EnumTypeDefinitionNodeBase
        : IHasDirectives
    {
        protected EnumTypeDefinitionNodeBase(
            Location location,
            NameNode name,
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
            Directives = directives;
            Values = values;
        }

        public abstract NodeKind Kind { get; }
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}
