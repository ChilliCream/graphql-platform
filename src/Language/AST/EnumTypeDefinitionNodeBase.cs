using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class EnumTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected EnumTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
            : base(location, name, directives)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Values = values;
        }

        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}
