using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class EnumTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IEnumTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IEnumTypeDescriptor descriptor);
    }
}
