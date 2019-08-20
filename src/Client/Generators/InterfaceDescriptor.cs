using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class InterfaceDescriptor
    {
        public InterfaceDescriptor(
            INamedType type,
            string name,
            IReadOnlyList<FieldInfo> fields)
        {
            Type = type;
            Name = name;
            Fields = fields;
        }

        public INamedType Type { get; }
        public string Name { get; }
        public IReadOnlyList<FieldInfo> Fields { get; }
    }
}
