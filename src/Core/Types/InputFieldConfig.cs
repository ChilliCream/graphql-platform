using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldConfig
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<SchemaContext, IInputType> Type { get; set; }

        public Func<SchemaContext, IValueNode> DefaultValue { get; set; }
    }
}
