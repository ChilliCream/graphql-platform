using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IInputClassDescriptor
        : ICodeDescriptor
    {
        InputObjectType Type { get; }

        IReadOnlyList<IInputFieldDescriptor> Arguments { get; }
    }
}
