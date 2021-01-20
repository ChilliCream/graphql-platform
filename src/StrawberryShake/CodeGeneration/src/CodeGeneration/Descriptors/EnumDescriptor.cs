using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumDescriptor : ICodeDescriptor
    {
        public EnumDescriptor(
            NameString name,
            string @namespace,
            IReadOnlyList<EnumValueDescriptor> elements)
        {
            Name = name;
            Namespace = @namespace;
            Values = elements;
        }

        public NameString Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<EnumValueDescriptor> Values { get; }
    }
}
