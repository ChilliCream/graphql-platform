using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumTypeDescription
        : TypeDescriptionBase
    {
        public EnumTypeDefinitionNode SyntaxNode { get; set; }

        public Type NativeType { get; set; }

        protected List<EnumValueDescription> Items { get; set; } =
            new List<EnumValueDescription>();

        public BindingBehavior ValueBindingBehavior { get; set; }
    }
}
