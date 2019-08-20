using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class InterfaceDescriptor
        : IInterfaceDescriptor
    {
        public InterfaceDescriptor(
            INamedType type,
            string name,
            IReadOnlyList<IFieldDescriptor> fields)
        {
            Type = type;
            Name = name;
            Fields = fields;
        }

        public INamedType Type { get; }
        public string Name { get; }
        public IReadOnlyList<IFieldDescriptor> Fields { get; }
    }
}
