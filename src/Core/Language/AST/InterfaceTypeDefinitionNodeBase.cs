using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InterfaceTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected InterfaceTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives)
        {
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public IReadOnlyList<FieldDefinitionNode> Fields { get; }
    }
}
