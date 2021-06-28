using System;
using System.Linq;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class EnumTypeBindingDefinition
    {
        public NameString TypeName { get; set; }

        public Type? RuntimeType { get; set; }

        public IBindableList<EnumValueDefinition> Values { get; } =
            new BindableList<EnumValueDefinition>();
    }
}
