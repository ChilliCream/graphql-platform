using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class SortInputTypeDescriptorAttribute
        : DescriptorAttribute
    {
        protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is ISortInputTypeDescriptor d &&
                element is Type t)
            {
                OnConfigure(context, d, t);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            ISortInputTypeDescriptor descriptor,
            Type type);
    }
}
