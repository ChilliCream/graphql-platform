using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public interface IHasDescriptorContext
    {
        IDescriptorContext Context { get; }
    }
}
