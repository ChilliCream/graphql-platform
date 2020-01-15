using System;
using HotChocolate.AspNetCore.Authorization;

namespace HotChocolate.Types
{
    public static class AuthorizeObjectTypeDescriptorExtensions
    {
        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor self,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(roles));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> self,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(roles));
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective());
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor self,
            string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectTypeDescriptor Authorize(
            this IObjectTypeDescriptor self,
            string policy,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy, roles));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective());
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> self,
            string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectTypeDescriptor<T> Authorize<T>(
            this IObjectTypeDescriptor<T> self,
            string policy,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy, roles));
        }
    }
}
