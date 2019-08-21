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
            IInterfaceDescriptor implements)
            : this(type, name, new[] { implements })
        {
        }

        public ClassDescriptor(
            INamedType type,
            string name,
            IReadOnlyList<IInterfaceDescriptor> implements)
        {
            Type = type;
            Name = name;
            Implements = implements;
        }

        public INamedType Type { get; }
        public string Name { get; }
        public IReadOnlyList<IInterfaceDescriptor> Implements { get; }
    }
}
