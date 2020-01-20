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
            this IObjectTypeDescriptor descriptor,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(apply: apply));
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor descriptor,
            string policy,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy, apply: apply));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(apply: apply));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> descriptor,
            string policy,
            ApplyPolicy apply = ApplyPolicy.BeforeResolver)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new AuthorizeDirective(policy, apply: apply));
        }
    }
}
