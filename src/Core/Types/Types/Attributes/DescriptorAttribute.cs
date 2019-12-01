using System;

namespace HotChocolate.Types
{
    public abstract class DescriptorAttribute
        : Attribute
    {
        internal protected abstract void TryConfigure(IDescriptor descriptor);
    }
}
