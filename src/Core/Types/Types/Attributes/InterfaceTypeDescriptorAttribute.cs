using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InterfaceTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInterfaceTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInterfaceTypeDescriptor descriptor);
    }
}
