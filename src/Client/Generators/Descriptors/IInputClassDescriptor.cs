using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IInputClassDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        InputObjectType Type { get; }

        IReadOnlyList<IInputFieldDescriptor> Fields { get; }
    }
}
