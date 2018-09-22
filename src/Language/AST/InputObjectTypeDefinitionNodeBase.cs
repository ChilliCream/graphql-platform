using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InputObjectTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected InputObjectTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<InputValueDefinitionNode> fields)
            : base(location, name, directives)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Fields = fields;
        }

        public IReadOnlyCollection<InputValueDefinitionNode> Fields { get; }
    }
}
