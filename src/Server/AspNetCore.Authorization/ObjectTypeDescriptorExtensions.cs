using System;
using HotChocolate.Types;

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
namespace HotChocolate.AspNetCore.Authorization
#endif
{
    public static class ObjectTypeDescriptorExtensions
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

#if !ASPNETCLASSIC
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
#endif
    }
}
