using System;
using HotChocolate.Types;

namespace HotChocolate.RateLimit
{
    public static class LimitObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Limit(
            this IObjectFieldDescriptor self, string policy)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Directive(new LimitDirective(policy));
        }
    }
}
