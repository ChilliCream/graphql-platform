using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal sealed class ExtendedType
        : IExtendedType
        , IEquatable<ExtendedType>
    {
        private List<IExtendedType>? _interfaces;

        public ExtendedType(
            Type type,
            bool isNullable,
            ExtendedTypeKind kind,
            bool isList = false,
            bool isNamedType = false,
            IReadOnlyList<IExtendedType>? typeArguments = null)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            IsNullable = isNullable;
            IsList = isList;
            Kind = kind;
            TypeArguments = typeArguments ?? Array.Empty<IExtendedType>();
            IsNamedType = kind == ExtendedTypeKind.Schema && isNamedType;

            if (kind == ExtendedTypeKind.Unknown || kind == ExtendedTypeKind.Extended)
            {
                IsList = IsListType(type);
            }

            if (kind == ExtendedTypeKind.Unknown)
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
        }

        public Type Type { get; }

        public Type? Definition { get; }

        public ExtendedTypeKind Kind { get; }

        public bool IsGeneric => Type.IsGenericType;

        public bool IsArray => Type.IsArray;

        public bool IsList { get; }

        public bool IsCollection => IsList || IsArray;

        public bool IsNamedType { get; }

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

        public static ExtendedType FromType(Type type)
        {
            return IsSchemaType(type)
                ? FromSchemaType(type)
                : FromSystemType(type);
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
                        ExtendedTypeKind.Unknown);
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
                        current.GetGenericTypeDefinition() == typeof(ListType<>),
                        typeArguments: new[] { extendedType });
                }
            }

            return extendedType!;
        }

        private static bool IsSchemaType(Type type)
        {
            if (IsNamedSchemaType(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                if (typeof(ListType<>) == definition
                    || typeof(NonNullType<>) == definition
                    || typeof(NativeType<>) == definition)
                {
                    return IsSchemaType(type.GetGenericArguments()[0]);
                }
            }

            return false;
        }

        private static bool IsNamedSchemaType(Type type)
        {
            return typeof(ScalarType).IsAssignableFrom(type) ||
               typeof(ObjectType).IsAssignableFrom(type) ||
               typeof(InterfaceType).IsAssignableFrom(type) ||
               typeof(EnumType).IsAssignableFrom(type) ||
               typeof(UnionType).IsAssignableFrom(type) ||
               typeof(InputObjectType).IsAssignableFrom(type);
        }

        private static Type RemoveNonEssentialTypes(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NativeType<>))
            {
                return RemoveNonEssentialTypes(type.GetGenericArguments()[0]);
            }
            return type;
        }

        private static bool IsListType(Type type) =>
            type.IsArray
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListType<>))
            || ImplementsListInterface(type);

        private static bool ImplementsListInterface(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListType<>))
            {
                return true;
            }

            if (type.IsInterface && IsSupportedCollectionInterface(type, true))
            {
                return true;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedCollectionInterface(Type type) =>
            IsSupportedCollectionInterface(type, false);

        private static bool IsSupportedCollectionInterface(
            Type type,
            bool allowEnumerable)
        {
            if (type.IsGenericType)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IReadOnlyCollection<>)
                    || typeDefinition == typeof(IReadOnlyList<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>)
                    || typeDefinition == typeof(IQueryable<>)
                    || typeDefinition == typeof(IAsyncEnumerable<>)
                    || typeDefinition == typeof(IObservable<>))
                {
                    return true;
                }

                if (allowEnumerable && typeDefinition == typeof(IEnumerable<>))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
