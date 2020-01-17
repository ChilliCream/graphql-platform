using System;
using HotChocolate.AspNetCore.Authorization;

namespace HotChocolate.Types
{
    public static class AuthorizeObjectTypeDescriptorExtensions
    {
        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor descriptor,
            params string[] roles)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(roles));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor,
            params string[] roles)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(roles));
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective());
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor descriptor,
            string policy)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor descriptor,
            string policy,
            params string[] roles)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy, roles));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective());
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor,
            string policy)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor,
            string policy,
            params string[] roles)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy, roles));
        }
    }
}
