using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class EnumDescriptor : ICodeDescriptor
    {
        public EnumDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<EnumValueDescriptor> elements)
        {
            Name = name;
            Namespace = @namespace;
            Values = elements;
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<EnumValueDescriptor> Values { get; }
    }
}
