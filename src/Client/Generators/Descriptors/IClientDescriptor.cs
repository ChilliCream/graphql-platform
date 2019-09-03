using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IClientDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        IReadOnlyList<IOperationDescriptor> Operations { get; }
    }
}
