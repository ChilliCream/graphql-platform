using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public class DescriptorTestBase
    {
        protected DescriptorTestBase()
        {
        }

        public IDescriptorContext Context { get; } =
            DescriptorContext.Create();
    }
}
