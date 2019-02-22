using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class EnumTypeDescription
        : TypeDescriptionBase<EnumTypeDefinitionNode>
    {
        public IBindableList<EnumValueDescription> Values { get; } =
            new BindableList<EnumValueDescription>();
    }
}
