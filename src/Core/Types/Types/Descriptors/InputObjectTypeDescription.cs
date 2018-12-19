using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputObjectTypeDescription
        : TypeDescriptionBase
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; set; }

        public Type ClrType { get; set; }

        public List<InputFieldDescription> Fields { get; set; }
            = new List<InputFieldDescription>();

        public BindingBehavior FieldBindingBehavior { get; set; }
    }
}
