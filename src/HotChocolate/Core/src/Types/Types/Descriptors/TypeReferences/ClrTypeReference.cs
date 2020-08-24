using System;
using HotChocolate.Internal;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    // TODO : rename to extended type ref
    public sealed class ClrTypeReference
        : TypeReference
        , IEquatable<ClrTypeReference>
    {
        public ClrTypeReference(
            IExtendedType type,
            TypeContext context,
            string? scope = null)
            : base(context, scope)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public IExtendedType Type { get; }

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
            return $"{Context}: {Type.OriginalType.GetTypeName()}";
        }

        public ClrTypeReference WithType(Type type) =>
            WithType(ExtendedType.FromType(type));

        public ClrTypeReference WithType(IExtendedType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return TypeReference.Create(
                type,
                Context,
                Scope);
        }

        public ClrTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return TypeReference.Create(
                Type,
                context,
                Scope);
        }

        public ClrTypeReference WithScope(string? scope = null)
        {
            return TypeReference.Create(
                Type,
                Context,
                scope);
        }

        public ClrTypeReference With(
            Optional<IExtendedType> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default)
        {
            if (type.HasValue && type.Value is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return TypeReference.Create(
                type.HasValue ? type.Value! : Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope);
        }
    }
}
