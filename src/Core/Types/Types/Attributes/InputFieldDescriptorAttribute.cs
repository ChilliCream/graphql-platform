using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInputFieldDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInputFieldDescriptor descriptor);
    }
}
