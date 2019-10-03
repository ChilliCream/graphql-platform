using System;
using System.Reflection;

namespace HotChocolate.Types.Filters
{
    public class FilterOperation
    {
        public FilterOperation(
            Type type,
            FilterOperationKind kind,
            PropertyInfo property)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Kind = kind;
            Property = property
                ?? throw new ArgumentNullException(nameof(property));

            if (typeof(ISingleFilter).IsAssignableFrom(Type))
            {
                ArrayBaseType = Type.GetGenericArguments()[0];
            }
            if (typeof(ISingleFilter).IsAssignableFrom(property.DeclaringType))
            {
                ArrayBaseType = property.DeclaringType.GetGenericArguments()[0];
            }
        }

        public Type Type { get; }

        public FilterOperationKind Kind { get; }

        public PropertyInfo Property { get; }

        public bool IsSimpleArrayType => ArrayBaseType != null;
        public Type ArrayBaseType { get; }
    }
}
