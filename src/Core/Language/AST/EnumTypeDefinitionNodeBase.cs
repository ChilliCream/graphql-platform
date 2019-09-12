using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class EnumTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected EnumTypeDefinitionNodeBase(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<EnumValueDefinitionNode> values)
            : base(location, name, directives)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public IReadOnlyList<EnumValueDefinitionNode> Values { get; }
    }
}
