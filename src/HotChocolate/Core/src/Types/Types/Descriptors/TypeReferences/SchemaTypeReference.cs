using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class SchemaTypeReference
        : TypeReference
        , IEquatable<SchemaTypeReference>
    {
        public SchemaTypeReference(
            ITypeSystemMember type,
            TypeContext? context = null,
            string? scope = null,
            bool[]? nullable = null)
            : base(context ?? InferTypeContext(type), scope, nullable)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public ITypeSystemMember Type { get; }

        public bool Equals(SchemaTypeReference? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!IsEqual(other))
            {
                return false;
            }

            return Type.Equals(other.Type);
        }

        public override bool Equals(ITypeReference? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is SchemaTypeReference str)
            {
                return Equals(str);
            }

            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is SchemaTypeReference str)
            {
                return Equals(str);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return base.GetHashCode() ^ Type.GetHashCode() * 397;
            }
        }

        public override string ToString()
        {
            return $"{Context}: {Type}";
        }

        public SchemaTypeReference WithType(ITypeSystemMember type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new SchemaTypeReference(
                type,
                Context,
                Scope,
                Nullable);
        }

        public SchemaTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return new SchemaTypeReference(
                Type,
                context,
                Scope,
                Nullable);
        }

        public SchemaTypeReference WithScope(string? scope = null)
        {
            return new SchemaTypeReference(
                Type,
                Context,
                scope,
                Nullable);
        }

        public SchemaTypeReference WithNullable(bool[]? nullable = null)
        {
            return new SchemaTypeReference(
                Type,
                Context,
                Scope,
                nullable);
        }

        public SchemaTypeReference With(
            Optional<ITypeSystemMember> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default(Optional<string>),
            Optional<bool[]?> nullable = default(Optional<bool[]>))
        {
            if (type.HasValue && type.Value is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new SchemaTypeReference(
                type.HasValue ? type.Value! : Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope,
                nullable.HasValue ? nullable.Value : Nullable);
        }

        internal static TypeContext InferTypeContext(object? type)
        {
            if (type is IType t)
            {
                INamedType namedType = t.NamedType();

                if (namedType.IsInputType() && namedType.IsOutputType())
                {
                    return TypeContext.None;
                }

                if (namedType.IsOutputType())
                {
                    return TypeContext.Output;
                }

                if (namedType.IsInputType())
                {
                    return TypeContext.Input;
                }
            }

            if (type is Type ts)
            {
                return InferTypeContext(ts);
            }

            return TypeContext.None;
        }

        internal static TypeContext InferTypeContext(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeof(IInputType).IsAssignableFrom(type)
                && typeof(IOutputType).IsAssignableFrom(type))
            {
                return TypeContext.None;
            }

            if (typeof(IOutputType).IsAssignableFrom(type))
            {
                return TypeContext.Output;
            }

            if (typeof(IInputType).IsAssignableFrom(type))
            {
                return TypeContext.Input;
            }

            return TypeContext.None;
        }
    }
}
