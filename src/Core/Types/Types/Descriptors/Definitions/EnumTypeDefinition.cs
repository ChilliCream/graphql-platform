using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class EnumTypeDefinition
        : TypeDescriptionBase<EnumTypeDefinitionNode>
    {
        public IBindableList<EnumValueDescription> Values { get; } =
            new BindableList<EnumValueDescription>();
    }
}
