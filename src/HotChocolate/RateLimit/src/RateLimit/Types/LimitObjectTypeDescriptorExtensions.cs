using System;
using HotChocolate.Types;

namespace HotChocolate.RateLimit
{
    public static class LimitObjectTypeDescriptorExtensions
    {
        public static IObjectTypeDescriptor<T> Limit<T>(
            this IObjectTypeDescriptor<T> self, string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new LimitDirective(policy));
        }
    }
}
