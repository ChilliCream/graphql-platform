using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class ObjectTypeDescriptorExtensions
    {
        public static void Ignore<T>(
            this IObjectTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> propertyOrMethod)
        {
            descriptor.Field(propertyOrMethod).Ignore();
        }
    }
}
