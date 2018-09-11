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

        public List<EnumValueDescription> Values { get; set; } =
            new List<EnumValueDescription>();

        public BindingBehavior ValueBindingBehavior { get; set; }

        public List<DirectiveDescription> Directives { get; set; } =
            new List<DirectiveDescription>();
    }
}
