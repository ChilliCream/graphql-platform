using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class InterfaceDescriptor
        : IInterfaceDescriptor
    {
        public InterfaceDescriptor(
            string name,
            INamedType type,
            IReadOnlyList<IFieldDescriptor> fields,
            IReadOnlyList<IInterfaceDescriptor> implements)
        {
            Type = type;
            Name = name;
            Fields = fields;
        }

        public string Name { get; }
        public INamedType Type { get; }
        public IReadOnlyList<IFieldDescriptor> Fields { get; }
        public IReadOnlyList<IInterfaceDescriptor> Implements { get; }
    }
}
