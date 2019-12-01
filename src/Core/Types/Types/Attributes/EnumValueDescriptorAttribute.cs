using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Field,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class EnumValueDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IEnumValueDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IEnumValueDescriptor descriptor);
    }
}
