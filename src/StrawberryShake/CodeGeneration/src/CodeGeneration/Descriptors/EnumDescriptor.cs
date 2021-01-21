using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class EnumDescriptor : ICodeDescriptor
    {
        public EnumDescriptor(
            NameString name,
            string @namespace,
            IReadOnlyList<EnumValueDescriptor> elements,
            string? underlyingType = null)
        {
            Name = name;
            Namespace = @namespace;
            Values = elements;
            UnderlyingType = underlyingType;
        }

        public NameString Name { get; }

        public string? UnderlyingType { get; }

        public string Namespace { get; }

        public IReadOnlyList<EnumValueDescriptor> Values { get; }
    }
}
