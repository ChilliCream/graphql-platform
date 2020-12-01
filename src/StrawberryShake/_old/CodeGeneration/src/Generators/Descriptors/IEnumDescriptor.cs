using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IEnumDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        IReadOnlyList<IEnumValueDescriptor> Values { get; }
    }
}
