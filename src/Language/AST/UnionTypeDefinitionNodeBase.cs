using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class UnionTypeDefinitionNodeBase
       : IHasDirectives
    {
        protected UnionTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Location = location;
            Name = name;
            Directives = directives;
            Types = types;
        }

        public abstract NodeKind Kind { get; }
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}
