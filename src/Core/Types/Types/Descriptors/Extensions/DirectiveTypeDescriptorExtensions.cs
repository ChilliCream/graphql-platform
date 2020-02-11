using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class DirectiveTypeDescriptorExtensions
    {
        public static IDirectiveTypeDescriptor<T> Ignore<T>(
            this IDirectiveTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> property,
            bool ignore = true)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            descriptor.Argument(property).Ignore(ignore);
            return descriptor;
        }
    }
}
