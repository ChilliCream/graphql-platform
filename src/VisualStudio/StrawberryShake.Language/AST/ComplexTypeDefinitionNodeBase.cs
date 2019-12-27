using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class ComplexTypeDefinitionNodeBase
        : NamedSyntaxNode
    {
        protected ComplexTypeDefinitionNodeBase(
            Location? location,
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
