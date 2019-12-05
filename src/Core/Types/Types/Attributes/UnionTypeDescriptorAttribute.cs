using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class UnionTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IUnionTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IUnionTypeDescriptor descriptor);
    }
}
