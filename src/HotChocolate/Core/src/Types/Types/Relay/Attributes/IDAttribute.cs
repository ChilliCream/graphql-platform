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
            switch (descriptor)
            {
                case IInputFieldDescriptor d when element is PropertyInfo:
                    d.ID(TypeName);
                    break;
                casfe IArgumentDescriptor d when element is ParameterInfo:
                    d.ID(TypeName);
                    break;
                case IObjectFieldDescriptor d when element is MemberInfo:
                    d.ID(TypeName);
                    break;
                case IInterfaceFieldDescriptor d when element is MemberInfo:
                    d.ID();
                    break;
            }
        }
    }
}
