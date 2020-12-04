using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class EnumDescriptor
        : ICodeDescriptor
    {
        public EnumDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<EnumElementDescriptor> elements)
        {
            Name = name;
            Namespace = @namespace;
            Elements = elements;
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<EnumElementDescriptor> Elements { get; }
    }
}
