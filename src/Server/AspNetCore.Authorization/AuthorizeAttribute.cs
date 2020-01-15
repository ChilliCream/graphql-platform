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
    public sealed class AuthorizeAttribute : DescriptorAttribute
    {
        public string? Policy { get; set; }

        public string[]? Roles { get; set; }

        public ExecuteResolver ExecuteResolver { get; set; } = ExecuteResolver.AfterPolicy;

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
                    executeResolver: ExecuteResolver);
            }
            else if (Roles is { })
            {
                return new AuthorizeDirective(
                    Roles,
                    executeResolver: ExecuteResolver);
            }
            else
            {
                return new AuthorizeDirective(
                    executeResolver: ExecuteResolver);
            }
        }
    }
}
