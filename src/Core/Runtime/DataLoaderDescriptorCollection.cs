using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    public class DataLoaderDescriptorCollection
        : StateObjectDescriptorCollection<string>
    {
        public DataLoaderDescriptorCollection(
            IEnumerable<DataLoaderDescriptor> descriptors)
            : base(descriptors)
        {
        }
    }
}
