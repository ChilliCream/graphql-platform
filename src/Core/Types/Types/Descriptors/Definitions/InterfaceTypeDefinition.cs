using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceTypeDefinition
        : TypeDescriptionBase<InterfaceTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public IBindableList<InterfaceFieldDefinition> Fields { get; } =
            new BindableList<InterfaceFieldDefinition>();
    }
}
