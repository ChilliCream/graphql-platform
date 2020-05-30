using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterOperation
    {
        private bool _singleFilterInitialized;
        private Type? _arrayBaseType;

        public FilterOperation(
            Type type,
            object filterKind,
            object kind,
            PropertyInfo property)
        {
            Type = type;
            FilterKind = filterKind;
            Kind = kind;
            Property = property;

            if (Property is { } &&
                Property.DeclaringType != null)
            {
                IsNullable = new NullableHelper(
                    Property.DeclaringType).GetPropertyInfo(Property).IsNullable;
            }
        }

        public Type Type { get; }

        public object FilterKind { get; }

        public object Kind { get; }

        public PropertyInfo Property { get; }

        public bool IsNullable { get; }

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
