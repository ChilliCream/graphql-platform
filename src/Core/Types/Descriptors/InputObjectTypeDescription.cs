using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputObjectTypeDescription
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Type NativeType { get; set; }

        public List<InputFieldDescription> Fields { get; set; }
            = new List<InputFieldDescription>();

        public BindingBehavior FieldBindingBehavior { get; set; }
    }
}
