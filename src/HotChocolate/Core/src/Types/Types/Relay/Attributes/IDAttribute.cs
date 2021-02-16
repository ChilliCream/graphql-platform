using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay
{
    [AttributeUsage(
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.Method)]
    public class IDAttribute : DescriptorAttribute
    {
        public IDAttribute(string? typeName = null)
        {
            TypeName = typeName;
        }

        public NameString TypeName { get; }

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IInputFieldDescriptor ifd &&
                element is PropertyInfo)
            {
                ifd.ID(TypeName);
            }

            if (descriptor is IArgumentDescriptor ad &&
                element is ParameterInfo)
            {
                ad.ID(TypeName);
            }

            if (descriptor is IObjectFieldDescriptor ofd &&
                element is MemberInfo)
            {
                ofd.ID(TypeName);
            }

            if (descriptor is IInterfaceFieldDescriptor infd &&
                element is MemberInfo)
            {
                infd.ID();
            }
        }
    }
}
