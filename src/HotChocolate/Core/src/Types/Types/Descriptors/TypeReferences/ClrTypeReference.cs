using System;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeReference
        : TypeReference
        , IEquatable<ClrTypeReference>
    {
        public ClrTypeReference(
            Type type,
            TypeContext context,
            string? scope = null,
            bool[]? nullable = null)
            : base(context, scope, nullable)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type Type { get; }

        public ClrTypeReference Rewrite()
        {
            if (Nullable is null)
            {
                return this;
            }

            var nullable = new Utilities.Nullable[Nullable.Length];

            for (var i = 0; i < Nullable.Length; i++)
            {
                nullable[i] = Nullable[i] ? Utilities.Nullable.Yes : Utilities.Nullable.No;
            }

            ExtendedType extendedType = ExtendedType.FromType(Type);
            return With(
                type: ExtendedTypeRewriter.Rewrite(extendedType, nullable),
                nullable: null);
        }

        public bool Equals(ClrTypeReference? other)
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

            return Type == other.Type;
        }

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

            return Type == other.Type.GetType();
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

            if (other is SchemaTypeReference s)
            {
                return Equals(s);
            }

            if (other is ClrTypeReference c)
            {
                return Equals(c);
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

            if (obj is ClrTypeReference c)
            {
                return Equals(c);
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
            return $"{Context}: {Type.GetTypeName()}";
        }

        public ClrTypeReference WithType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return TypeReference.Create(
                type,
                Context,
                Scope,
                Nullable);
        }

        public ClrTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return TypeReference.Create(
                Type,
                context,
                Scope,
                Nullable);
        }

        public ClrTypeReference WithScope(string? scope = null)
        {
            return TypeReference.Create(
                Type,
                Context,
                scope,
                Nullable);
        }

        public ClrTypeReference WithNullable(bool[]? nullable = null)
        {
            return TypeReference.Create(
                Type,
                Context,
                Scope,
                nullable);
        }

        public ClrTypeReference With(
            Optional<Type> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default)
        {
            if (type.HasValue && type.Value is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return TypeReference.Create(
                type.HasValue ? type.Value! : Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope,
                nullable.HasValue ? nullable.Value : Nullable);
        }
    }
}
