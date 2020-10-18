using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class FilterFieldDescriptorAttribute
        : DescriptorAttribute
    {
        protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IFilterFieldDescriptor d &&
                element is MemberInfo m)
            {
                OnConfigure(context, d, m);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            IFilterFieldDescriptor descriptor,
            MemberInfo member);
    }
}
