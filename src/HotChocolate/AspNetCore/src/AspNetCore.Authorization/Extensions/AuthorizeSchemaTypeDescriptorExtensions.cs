using System;
using HotChocolate.AspNetCore.Authorization;

namespace HotChocolate.Types
{
    public static class AuthorizeSchemaTypeDescriptorExtensions
    {
        public static ISchemaTypeDescriptor Authorize(
            this ISchemaTypeDescriptor self,
            params string[] roles)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(roles));
        }

        public static ISchemaTypeDescriptor Authorize(
            this ISchemaTypeDescriptor self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective());
        }

        public static ISchemaTypeDescriptor Authorize(
            this ISchemaTypeDescriptor self,
            string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new AuthorizeDirective(policy));
        }

        public static ISchemaTypeDescriptor Authorize(
            this ISchemaTypeDescriptor self,
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
