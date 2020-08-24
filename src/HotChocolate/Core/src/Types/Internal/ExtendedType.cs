using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
        : IExtendedType
        , IEquatable<ExtendedType>
    {
        private readonly IExtendedType? _elementType;
        private List<IExtendedType>? _interfaces;

        internal ExtendedType(
            Type type,
            bool isNullable,
            ExtendedTypeKind kind,
            bool isList = false,
            bool isNamedType = false,
            IReadOnlyList<IExtendedType>? typeArguments = null,
            Type? originalType = null,
            IExtendedType? elementType = null)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            IsNullable = isNullable;
            IsList = isList;
            Kind = kind;
            TypeArguments = typeArguments ?? Array.Empty<IExtendedType>();
            IsNamedType = kind == ExtendedTypeKind.Schema && isNamedType;
            _elementType = elementType;

            if (!IsArrayOrList &&
                (kind == ExtendedTypeKind.Unknown || kind == ExtendedTypeKind.Extended))
            {
                IsList = IsListType(type);
            }

            if (kind == ExtendedTypeKind.Unknown && typeArguments is null)
            {
                if (type.IsGenericType && TypeArguments.Count == 0)
                {
                    Type[] arguments = type.GetGenericArguments();
                    var extendedArguments = new IExtendedType[arguments.Length];
                    for (var i = 0; i < extendedArguments.Length; i++)
                    {
                        extendedArguments[i] = FromType(arguments[i]);
                    }
                    TypeArguments = extendedArguments;
                }
                else if (type.IsArray)
                {
                    // legacy behavior -> remove
                    TypeArguments = new IExtendedType[]
                    {
                        FromType(type.GetElementType()!)
                    };
                }
            }

            if (type.IsGenericType)
            {
                Definition = type.GetGenericTypeDefinition();
            }

            if (_elementType is null)
            {
                if (IsArray)
                {
                    _elementType = TypeArguments[0];
                }

                if (IsList)
                {
                    _elementType = GetInnerListType(this)!;
                }
            }

            if (originalType is not null)
            {
                OriginalType = originalType;
            }
            else if (type.IsValueType && IsNullable)
            {
                OriginalType = typeof(Nullable<>).MakeGenericType(type);
            }
            else
            {
                OriginalType = type;
            }
        }

        public Type Type { get; }

        public Type OriginalType { get; }

        public Type? Definition { get; }

        public ExtendedTypeKind Kind { get; }

        public bool IsGeneric => Type.IsGenericType;

        public bool IsArray => Type.IsArray;

        public bool IsList { get; }

        public bool IsArrayOrList => IsList || IsArray;

        public bool IsNamedType { get; }

        public bool IsSchemaType => Kind == ExtendedTypeKind.Schema;

        public bool IsInterface => Type.IsInterface;

        public bool IsNullable { get; }

        public IReadOnlyList<IExtendedType> TypeArguments { get; }

        public IReadOnlyList<IExtendedType> GetInterfaces()
        {
            if (_interfaces is null)
            {
                var types = new List<IExtendedType>();
                foreach (Type type in Type.GetInterfaces())
                {
                    types.Add(FromSystemType(type));
                }
                _interfaces = types;
            }
            return _interfaces;
        }

        public IExtendedType? GetElementType() => _elementType;

        public bool Equals(ExtendedType? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other.Type == Type
                && other.Kind.Equals(Kind)
                && other.IsNullable.Equals(IsNullable)
                && other.TypeArguments.Count == TypeArguments.Count)
            {
                for (var i = 0; i < other.TypeArguments.Count; i++)
                {
                    if (!other.TypeArguments[i].Equals(TypeArguments[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public bool Equals(IExtendedType? other) =>
            Equals(other as ExtendedType);

        public override bool Equals(object? obj) =>
            Equals(obj as ExtendedType);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Type.GetHashCode() * 397)
                   ^ (Kind.GetHashCode() * 397)
                   ^ (IsNullable.GetHashCode() * 397);

                for (var i = 0; i < TypeArguments.Count; i++)
                {
                    hashCode ^= (TypeArguments[i].GetHashCode() * 397 * i);
                }

                return hashCode;
            }
        }

        public static ExtendedType FromType(Type type) =>
            TypeCache.GetOrCreateType(type, () => FromTypeInternal(type));

        private static ExtendedType FromTypeInternal(Type type)
        {
            return IsSchemaTypeInternal(type)
                ? FromSchemaType(type)
                : FromSystemType(type);
        }

        public static ExtendedType FromExtendedType(IExtendedType type, MemberInfo? member = null)
        {
            if (type.Kind == ExtendedTypeKind.Extended)
            {
                return member is null
                    ? TypeCache.GetOrCreateType(() => FromExtendedTypeInternal(type))
                    : TypeCache.GetOrCreateType(member, () => FromExtendedTypeInternal(type));
            }

            throw new NotSupportedException("Kind must be extended.");
        }

        public static ExtendedType FromExtendedType(IExtendedType type, ParameterInfo member)
        {
            if (type.Kind == ExtendedTypeKind.Extended)
            {
                return TypeCache.GetOrCreateType(member, () => FromExtendedTypeInternal(type));
            }

            throw new NotSupportedException("Kind must be extended.");
        }

        private static ExtendedType FromExtendedTypeInternal(IExtendedType type)
        {
            type = RemoveNonEssentialTypes(type);

            IReadOnlyList<IExtendedType> arguments = type.TypeArguments;
            var extendedArguments = new IExtendedType[arguments.Count];

            for (var i = 0; i < extendedArguments.Length; i++)
            {
                extendedArguments[i] = FromExtendedType(arguments[i]);
            }

            IExtendedType? elementType = null;
            var isList = !type.IsArray && IsSupportedCollectionInterface(type.Type);

            if (isList && type.TypeArguments.Count == 1)
            {
                Type a = GetInnerListType(type.Type)!;
                IExtendedType b = type.TypeArguments[0];
                if (a == b.Type || a == b.OriginalType)
                {
                    elementType = b;
                }
            }

            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }

            return new ExtendedType(
                type.Type,
                type.IsNullable,
                ExtendedTypeKind.Unknown,
                isList,
                false,
                extendedArguments,
                type.OriginalType,
                elementType);
        }

        private static ExtendedType FromSystemType(Type type)
        {
            type = RemoveNonEssentialTypes(type);

            if (type.IsValueType)
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return new ExtendedType(
                        type.GetGenericArguments()[0],
                        true,
                        ExtendedTypeKind.Unknown,
                        originalType: type);
                }
                return new ExtendedType(type, false, ExtendedTypeKind.Unknown);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NonNullType<>))
            {
                return new ExtendedType(
                    RemoveNonEssentialTypes(type.GetGenericArguments()[0]),
                    false,
                    ExtendedTypeKind.Unknown);
            }

            return new ExtendedType(type, true, ExtendedTypeKind.Unknown);
        }

        private static ExtendedType FromSchemaType(Type type)
        {
            var components = new Stack<Type>();
            Type? current = type;

            while (current != null)
            {
                if (current.IsGenericType)
                {
                    Type definition = current.GetGenericTypeDefinition();
                    if (definition == typeof(ListType<>)
                        || definition == typeof(NonNullType<>))
                    {
                        components.Push(current);
                        current = current.GetGenericArguments()[0];
                    }
                    else
                    {
                        components.Push(current);
                        current = null;
                    }
                }
                else
                {
                    components.Push(current);
                    current = null;
                }
            }

            ExtendedType? extendedType = null;

            while (components.Count > 0)
            {
                var nullable = true;
                current = components.Pop();

                if (components.Count > 0
                    && components.Peek().GetGenericTypeDefinition() == typeof(NonNullType<>))
                {
                    nullable = false;
                    components.Pop();
                }

                if (extendedType is null)
                {
                    extendedType = new ExtendedType(
                        current,
                        nullable,
                        ExtendedTypeKind.Schema,
                        isNamedType: true);
                }
                else
                {
                    extendedType = new ExtendedType(
                        current,
                        nullable,
                        ExtendedTypeKind.Schema,
                        isList: true,
                        typeArguments: new[] { extendedType },
                        elementType: extendedType);
                }
            }

            return extendedType!;
        }
    }
}
