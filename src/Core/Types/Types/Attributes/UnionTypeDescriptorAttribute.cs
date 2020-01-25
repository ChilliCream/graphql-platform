using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class UnionTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IUnionTypeDescriptor d
                && element is Type t)
            {
                OnConfigure(context, d, t);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            IUnionTypeDescriptor descriptor,
            Type type);
    }
}
