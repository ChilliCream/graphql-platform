using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InputObjectTypeDefinitionNode
        : InputObjectTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public InputObjectTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<InputValueDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.InputObjectTypeDefinition;
        public StringValueNode Description { get; }
    }
}
