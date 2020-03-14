using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class InterfaceTypeDescriptorExtensions
    {
        public static IInterfaceTypeDescriptor<T> Ignore<T>(
            this IInterfaceTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> propertyOrMethod,
            bool ignore = true)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (propertyOrMethod == null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            descriptor.Field(propertyOrMethod).Ignore(ignore);
            return descriptor;
        }
    }
}
