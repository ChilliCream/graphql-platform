using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputObjectTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInputObjectTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInputObjectTypeDescriptor descriptor);
    }
}
