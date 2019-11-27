using System;

#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif

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

#if !ASPNETCLASSIC
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
#endif
    }
}
