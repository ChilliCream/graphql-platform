using System;

namespace HotChocolate.Types
{
    public abstract class DescriptorAttribute
        : Attribute
    {
        internal abstract void TryConfigure(IDescriptor descriptor);
    }

}
