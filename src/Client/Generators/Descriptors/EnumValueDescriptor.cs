using System;
using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public class EnumValueDescriptor
        : IEnumValueDescriptor
    {
        public EnumValueDescriptor(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Value { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }


}
