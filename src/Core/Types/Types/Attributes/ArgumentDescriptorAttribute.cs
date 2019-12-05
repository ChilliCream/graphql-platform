using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ArgumentDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IArgumentDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IArgumentDescriptor descriptor);
    }
}
