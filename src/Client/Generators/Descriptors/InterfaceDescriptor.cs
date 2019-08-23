using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types;
using System;

namespace StrawberryShake.Generators
{
    public class InterfaceDescriptor
        : IInterfaceDescriptor
    {
        public InterfaceDescriptor(string name, INamedType type)
            : this(
                name,
                type,
                Array.Empty<IFieldDescriptor>(),
                Array.Empty<IInterfaceDescriptor>())
        {
        }

        public InterfaceDescriptor(
            string name,
            INamedType type,
            IReadOnlyList<IFieldDescriptor> fields)
            : this(
                name,
                type,
                fields,
                Array.Empty<IInterfaceDescriptor>())
        {
        }

        public InterfaceDescriptor(
            string name,
            INamedType type,
            IReadOnlyList<IFieldDescriptor> fields,
            IReadOnlyList<IInterfaceDescriptor> implements)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Implements = implements ?? throw new ArgumentNullException(nameof(implements));
        }

        public string Name { get; }

        public INamedType Type { get; }

        public IReadOnlyList<IInterfaceDescriptor> Implements { get; }

        public IReadOnlyList<IFieldDescriptor> Fields { get; }

        public InterfaceDescriptor TryAddImplements(IInterfaceDescriptor descriptor)
        {
            var implements = new Dictionary<string, IInterfaceDescriptor>();

            foreach (IInterfaceDescriptor d in Implements)
            {
                implements[d.Name] = d;
            }

            implements[descriptor.Name] = descriptor;

            return new InterfaceDescriptor(
                Name,
                Type,
                Fields,
                implements.Values.ToList());
        }

        public InterfaceDescriptor RemoveAllImplements()
        {
            return new InterfaceDescriptor(
                Name,
                Type,
                Fields,
                Array.Empty<InterfaceDescriptor>());
        }

        IInterfaceDescriptor IInterfaceDescriptor.TryAddImplements(
            IInterfaceDescriptor descriptor) =>
            TryAddImplements(descriptor);

        IInterfaceDescriptor IInterfaceDescriptor.RemoveAllImplements() =>
            RemoveAllImplements();

        IEnumerable<ICodeDescriptor> ICodeDescriptor.GetChildren() =>
            Implements;
    }
}
