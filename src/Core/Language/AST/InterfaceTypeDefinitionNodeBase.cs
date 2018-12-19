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
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives)
        {
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}
