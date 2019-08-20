using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ClassDescriptor
        : IClassDescriptor
    {
        public ClassDescriptor(
            INamedType type,
            string name,
            IReadOnlyList<IInterfaceDescriptor> fields)
        {
            Type = type;
            Name = name;
            Fields = fields;
        }

        public INamedType Type { get; }
        public string Name { get; }
        public IReadOnlyList<IInterfaceDescriptor> Fields { get; }
    }
}
