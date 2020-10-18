using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class FilterInputTypeDescriptorAttribute
        : DescriptorAttribute
    {
        protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IFilterInputTypeDescriptor d &&
                element is Type t)
            {
                OnConfigure(context, d, t);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            IFilterInputTypeDescriptor descriptor,
            Type type);
    }
}
