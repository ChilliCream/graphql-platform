using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IObjectFieldDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IObjectFieldDescriptor descriptor);
    }
}
