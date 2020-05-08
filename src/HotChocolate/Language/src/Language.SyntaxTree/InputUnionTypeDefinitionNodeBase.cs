using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InputUnionTypeDefinitionNodeBase
       : NamedSyntaxNode
    {
        protected InputUnionTypeDefinitionNodeBase(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> types)
            : base(location, name, directives)
        {
            Types = types ?? throw new ArgumentNullException(nameof(types));
        }

        public IReadOnlyList<NamedTypeNode> Types { get; }
    }
}
