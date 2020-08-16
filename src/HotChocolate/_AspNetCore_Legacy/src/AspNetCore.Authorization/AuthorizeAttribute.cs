using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.AspNetCore.Authorization
{
    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Property
        | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public class AuthorizeAttribute : DescriptorAttribute
    {
        public string? Policy { get; set; }

        public string[]? Roles { get; set; }

        public ApplyPolicy Apply { get; set; } = ApplyPolicy.BeforeResolver;

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectTypeDescriptor type)
            {
                type.Directive(CreateDirective());
            }
            else if (descriptor is IObjectFieldDescriptor field)
            {
                field.Directive(CreateDirective());
            }
        }

        private AuthorizeDirective CreateDirective()
        {
            if (Policy is { })
            {
                return new AuthorizeDirective(
                    Policy,
                    apply: Apply);
            }
            else if (Roles is { })
            {
                return new AuthorizeDirective(
                    Roles,
                    apply: Apply);
            }
            else
            {
                return new AuthorizeDirective(
                    apply: Apply);
            }
        }
    }
}
