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
    public class GlobalIdAttribute : DescriptorAttribute
    {
        public GlobalIdAttribute(string? typeName = null)
        {
            if (typeName is not null)
            {
                TypeName = typeName;
            }
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
                    d.GlobalId(TypeName);
                    break;
                case IArgumentDescriptor d when element is ParameterInfo:
                    d.GlobalId(TypeName);
                    break;
                case IObjectFieldDescriptor d when element is MemberInfo:
                    d.GlobalId(TypeName);
                    break;
                case IInterfaceFieldDescriptor d when element is MemberInfo:
                    d.GlobalId();
                    break;
            }
        }
    }
}
