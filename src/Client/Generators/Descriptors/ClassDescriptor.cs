using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class ClassDescriptor
        : IClassDescriptor
    {
        public ClassDescriptor(
            string name,
            INamedType type,
            IInterfaceDescriptor implements)
            : this(name, type, new[] { implements })
        {
        }

        public ClassDescriptor(
            string name,
            INamedType type,
            IReadOnlyList<IInterfaceDescriptor> implements)
        {
            Name = name;
            Type = type;
            Implements = implements;
        }

        public string Name { get; }

        public INamedType Type { get; }

        public IReadOnlyList<IInterfaceDescriptor> Implements { get; }

        IEnumerable<ICodeDescriptor> ICodeDescriptor.GetChildren() =>
            Implements;
    }
}
