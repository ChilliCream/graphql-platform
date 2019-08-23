using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IInterfaceDescriptor
        : ICodeDescriptor
    {
        INamedType Type { get; }

        IReadOnlyList<IInterfaceDescriptor> Implements { get; }

        IReadOnlyList<IFieldDescriptor> Fields { get; }

        IInterfaceDescriptor TryAddImplements(IInterfaceDescriptor descriptor);

        IInterfaceDescriptor RemoveAllImplements();
    }
}
