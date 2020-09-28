using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class InputObjectTypeDescriptorExtensions
    {
        public static IInputObjectTypeDescriptor<T> Ignore<T>(
            this IInputObjectTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> property)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            descriptor.Field(property).Ignore();
            return descriptor;
        }
    }
}
