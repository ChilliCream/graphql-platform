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
        }

        public Type Type { get; }

        public FilterOperationKind Kind { get; }

        public PropertyInfo Property { get; }
    }
}
