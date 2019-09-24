using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IEnumDescriptor
        : ICodeDescriptor
    {
        IReadOnlyList<IEnumValueDescriptor> Values { get; }
    }

    public interface IEnumValueDescriptor
        : ICodeDescriptor
    {
        string Value { get; }
    }
}
