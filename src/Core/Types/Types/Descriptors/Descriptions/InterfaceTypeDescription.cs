using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceTypeDescription
        : TypeDescriptionBase<InterfaceTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public IBindableList<InterfaceFieldDescription> Fields { get; } =
            new BindableList<InterfaceFieldDescription>();
    }
}
