using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IInterfaceDescriptor
    {
        INamedType Type { get; }
        string Name { get; }
        IReadOnlyList<IFieldDescriptor> Fields { get; }
    }
}
