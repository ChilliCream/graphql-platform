using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterOperation
    {
        private readonly bool _isMetaField;

        public FilterOperation(
            Type type,
            int filterKind,
            int kind,
            PropertyInfo property)
        {
            Type = type;
            FilterKind = filterKind;
            Kind = kind;
            Property = property;
            _isMetaField = Property?.GetCustomAttribute(typeof(FilterMetaFieldAttribute)) is { };

            if (Property is { } &&
                Property.DeclaringType != null)
            {
                IsNullable = new NullableHelper(
                    Property.DeclaringType).GetPropertyInfo(Property).IsNullable;
            }
        }

        public FilterOperation(
            Type type,
            int filterKind,
            int kind,
            PropertyInfo property,
            Type? elementType)
            : this(type, filterKind, kind, property)
        {
            ElementType = elementType;
        }

        public Type Type { get; }

        public Type? ElementType { get; }

        public int FilterKind { get; }

        public int Kind { get; }

        public PropertyInfo Property { get; }

        public bool IsNullable { get; }

        public bool TryGetElementType(
            [NotNullWhen(true)] out Type? baseType)
        {
            baseType = ElementType;
            return baseType != null;
        }

        public bool IsMetaField() => _isMetaField || TryGetElementType(out _);

        public FilterOperation WithOperationKind(int kind) =>
            new FilterOperation(Type, FilterKind, kind, Property);

        public FilterOperation WithFilterKind(int filterKind) =>
            new FilterOperation(Type, filterKind, Kind, Property);

        public FilterOperation WithType(Type type) =>
            new FilterOperation(type, FilterKind, Kind, Property);

        public FilterOperation WithProperty(PropertyInfo property) =>
            new FilterOperation(Type, FilterKind, Kind, property);
    }
}
