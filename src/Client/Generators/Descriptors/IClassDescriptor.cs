using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IClassDescriptor
    {
        INamedType Type { get; }
        string Name { get; }
        IReadOnlyList<IInterfaceDescriptor> Fields { get; }
    }
}
