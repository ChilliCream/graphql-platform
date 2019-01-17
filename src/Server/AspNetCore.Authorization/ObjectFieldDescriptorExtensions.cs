using System;

#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif

namespace HotChocolate.Types
{
    public static class ObjectFieldDescriptorExtensions
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

#if !ASPNETCLASSIC
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
#endif
    }
}
