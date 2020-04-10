using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HotChocolate.Types.Filters
{
    public class FilterOperation
    {
        private bool _singleFilterInitialized;
        private Type? _arrayBaseType;

        public FilterOperation(
            Type type,
            FilterKind filterKind,
            FilterOperationKind kind,
            PropertyInfo property)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            FilterKind = filterKind;
            Kind = kind;
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public Type Type { get; }

        public FilterKind FilterKind { get; }

        public FilterOperationKind Kind { get; }

        public PropertyInfo Property { get; }

        public bool TryGetSimpleFilterBaseType(
            [NotNullWhen(true)]out Type? baseType)
        {
            if (!_singleFilterInitialized)
            {
                if (typeof(ISingleFilter).IsAssignableFrom(Type))
                {
                    _arrayBaseType = Type.GetGenericArguments()[0];
                }
                if (typeof(ISingleFilter).IsAssignableFrom(Property.DeclaringType) &&
                    Property.DeclaringType is { })
                {
                    _arrayBaseType = Property.DeclaringType.GetGenericArguments()[0];
                }
                _singleFilterInitialized = true;
            }

            baseType = _arrayBaseType;
            return baseType != null;
        }

        public bool IsSimpleArrayType() => TryGetSimpleFilterBaseType(out _);
    }
}
