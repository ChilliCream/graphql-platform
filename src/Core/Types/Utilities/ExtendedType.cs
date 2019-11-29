using System;
using System.Collections.Generic;
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
            ExtendedTypeKind kind)
            : this(type, isNullable, kind, Array.Empty<IExtendedType>())
        {
        }

        public ExtendedType(
            Type type,
            bool isNullable,
            ExtendedTypeKind kind,
            IReadOnlyList<IExtendedType> typeArguments)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            IsNullable = isNullable;
            Kind = kind;
            TypeArguments = typeArguments ?? throw new ArgumentNullException(nameof(typeArguments));

            if (kind == ExtendedTypeKind.Unknown)
            {
                if (type.IsGenericType && typeArguments.Count == 0)
                {
                    Type[] arguments = type.GetGenericArguments();
                    var extendedArguments = new IExtendedType[arguments.Length];
                    for (int i = 0; i < extendedArguments.Length; i++)
                    {
                        extendedArguments[i] = FromType(arguments[i]);
                    }
                    TypeArguments = extendedArguments;
                }
                else if (type.IsArray)
                {
                    TypeArguments = new IExtendedType[]
                    {
                        FromType(type.GetElementType())
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

            if (other.Type.Equals(Type)
                && other.Kind.Equals(Kind)
                && other.IsNullable.Equals(IsNullable)
                && other.TypeArguments.Count == TypeArguments.Count)
            {
                for (int i = 0; i < other.TypeArguments.Count; i++)
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

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as ExtendedType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Type.GetHashCode() * 397)
                    ^ (Kind.GetHashCode() * 397)
                    ^ (IsNullable.GetHashCode() * 397);

                for (int i = 0; i < TypeArguments.Count; i++)
                {
                    hashCode ^= (TypeArguments[i].GetHashCode() * 397);
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
                bool nullable = true;
                current = components.Pop();

                if (components.Count > 0
                    && components.Peek().GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    nullable = false;
                    components.Pop();
                }

                if (extendedType is null)
                {
                    extendedType = new ExtendedType(
                        current,
                        nullable,
                        ExtendedTypeKind.Schema);
                }
                else
                {
                    extendedType = new ExtendedType(
                        current,
                        nullable,
                        ExtendedTypeKind.Schema,
                        new[] { extendedType });
                }
            }

            return extendedType!;
        }

        private static bool IsSchemaType(Type type)
        {
            return typeof(ScalarType).IsAssignableFrom(type)
                || typeof(ObjectType).IsAssignableFrom(type)
                || typeof(InterfaceType).IsAssignableFrom(type)
                || typeof(EnumType).IsAssignableFrom(type)
                || typeof(UnionType).IsAssignableFrom(type)
                || typeof(InputObjectType).IsAssignableFrom(type)
                || type.IsGenericType
                    && (typeof(ListType<>) == type.GetGenericTypeDefinition()
                        || typeof(NonNullType<>) == type.GetGenericTypeDefinition());
        }
    }
}
