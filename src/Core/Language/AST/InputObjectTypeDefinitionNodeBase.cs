using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InputObjectTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected InputObjectTypeDefinitionNodeBase(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<InputValueDefinitionNode> fields)
            : base(location, name, directives)
        {
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public IReadOnlyList<InputValueDefinitionNode> Fields { get; }
    }
}
