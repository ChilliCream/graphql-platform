using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InterfaceFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IInterfaceFieldDescriptor d
                && element is MemberInfo m)
            {
                OnConfigure(context, d, m);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            IInterfaceFieldDescriptor descriptor,
            MemberInfo member);
    }
}
