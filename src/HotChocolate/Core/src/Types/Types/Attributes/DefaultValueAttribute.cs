using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Parameter | AttributeTargets.Property,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class DefaultValueAttribute
        : DescriptorAttribute
    {
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; }

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IArgumentDescriptor arg)
            {
                arg.DefaultValue(Value);
            }

            if (descriptor is IDirectiveArgumentDescriptor darg)
            {
                darg.DefaultValue(Value);
            }

            if (descriptor is IInputFieldDescriptor field)
            {
                field.DefaultValue(Value);
            }
        }
    }
}
