using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IInterfaceDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        INamedType Type { get; }

        IReadOnlyList<IInterfaceDescriptor> Implements { get; }

        IReadOnlyList<IFieldDescriptor> Fields { get; }

        IInterfaceDescriptor TryAddImplements(IInterfaceDescriptor descriptor);

        IInterfaceDescriptor RemoveAllImplements();
    }
}
