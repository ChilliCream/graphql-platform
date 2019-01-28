using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescription
        : TypeDescriptionBase
    {
        public Type ClrType { get; set; }

        public BindingBehavior FieldBindingBehavior { get; set; }

        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public ResolveAbstractType ResolveAbstractType { get; set; }

        public List<InterfaceFieldDescription> Fields { get; set; } =
            new List<InterfaceFieldDescription>();
    }
}
