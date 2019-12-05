using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InterfaceFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInterfaceFieldDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInterfaceFieldDescriptor descriptor);
    }
}
