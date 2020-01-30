using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Field,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class EnumValueDescriptorAttribute
        : DescriptorAttribute
    {
        internal protected sealed override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IEnumValueDescriptor d
                && element is FieldInfo f)
            {
                OnConfigure(context, d, f);
            }
        }

        public abstract void OnConfigure(
            IDescriptorContext context,
            IEnumValueDescriptor descriptor,
            FieldInfo field);
    }
}
