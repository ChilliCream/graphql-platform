using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public static class InterfaceTypeDescriptorExtensions
    {
        public static void Ignore<T>(
            this IInterfaceTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> propertyOrMethod)
        {
            descriptor.Field(propertyOrMethod).Ignore();
        }
    }
}
