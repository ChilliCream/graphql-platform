using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IObjectTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IObjectTypeDescriptor descriptor);
    }

}
