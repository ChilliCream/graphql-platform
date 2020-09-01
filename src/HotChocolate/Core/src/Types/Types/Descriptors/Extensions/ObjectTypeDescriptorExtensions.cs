using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class ObjectTypeDescriptorExtensions
    {
        public static IObjectTypeDescriptor<T> Ignore<T>(
            this IObjectTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> propertyOrMethod)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            descriptor.Field(propertyOrMethod).Ignore();
            return descriptor;
        }
    }
}
