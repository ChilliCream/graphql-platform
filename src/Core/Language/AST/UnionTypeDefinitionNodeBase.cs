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
            Types = types ?? throw new ArgumentNullException(nameof(types));
        }

        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}
