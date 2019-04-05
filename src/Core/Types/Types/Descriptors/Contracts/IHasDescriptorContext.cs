using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    internal interface IHasDescriptorContext
    {
        IDescriptorContext Context { get; }
    }
}
