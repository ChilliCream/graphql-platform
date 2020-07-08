using System;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeReference
        : TypeReference
        , IClrTypeReference
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

        public IClrTypeReference Rewrite()
        {
            if (Nullable is null)
            {
                return this;
            }

            var nullable = new Utilities.Nullable[Nullable.Length];

            for (int i = 0; i < Nullable.Length; i++)
            {
                nullable[i] = Nullable[i] ? Utilities.Nullable.Yes : Utilities.Nullable.No;
            }

            ExtendedType extendedType = ExtendedType.FromType(Type);
            return WithType(ExtendedTypeRewriter.Rewrite(extendedType, nullable));
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

            return Type.Equals(other.Type);
        }

        public bool Equals(IClrTypeReference? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is ClrTypeReference c)
            {
                return Equals(c);
            }

            return false;
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

        public IClrTypeReference WithType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new ClrTypeReference(
                type,
                Context,
                Scope,
                Nullable);
        }

        public IClrTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return new ClrTypeReference(
                Type,
                context,
                Scope,
                Nullable);
        }

        public IClrTypeReference WithScope(string? scope = null)
        {
            return new ClrTypeReference(
                Type,
                Context,
                scope,
                Nullable);
        }

        public IClrTypeReference WithNullable(bool[]? nullable = null)
        {
            return new ClrTypeReference(
                Type,
                Context,
                Scope,
                nullable);
        }

        public IClrTypeReference With(
            Optional<Type> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default)
        {
            if (type.HasValue && type.Value is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new ClrTypeReference(
                type.HasValue ? type.Value! : Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope,
                nullable.HasValue ? nullable.Value : Nullable);
        }
    }
}
