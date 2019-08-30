using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IClientDescriptor
        : ICodeDescriptor
    {
        IReadOnlyList<IOperationDescriptor> Operations { get; }
    }
}
