using System;
using HotChocolate.AspNetCore.Authorization;

namespace HotChocolate.Types
{
    public static class AuthorizeObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor descriptor,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective());
        }

        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor descriptor,
            string policy,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor descriptor,
            params string[] roles)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(roles));
        }
    }
}
