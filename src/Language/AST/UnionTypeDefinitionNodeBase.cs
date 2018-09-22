using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class UnionTypeDefinitionNodeBase
       : NamedSyntaxNode
    {
        protected UnionTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
            : base(location, name, directives)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Types = types;
        }

        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}
