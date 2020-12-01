using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class EnumDescriptor
        : ICodeDescriptor
    {
        public EnumDescriptor(string name, IReadOnlyList<EnumElementDescriptor> elements)
        {
            Name = name;
            Elements = elements;
        }

        public string Name { get; }

        public IReadOnlyList<EnumElementDescriptor> Elements { get; }
    }
}
