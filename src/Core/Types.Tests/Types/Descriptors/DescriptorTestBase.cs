using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

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
