using System;
using HotChocolate.AspNetCore.Authorization;

namespace HotChocolate.Types
{
    public static class AuthorizeObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor self,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(roles));
        }

        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective());
        }

        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor self,
            string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy));
        }

        public static IObjectFieldDescriptor Authorize(
            this IObjectFieldDescriptor self,
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
