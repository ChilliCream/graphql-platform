using System;
using HotChocolate.Internal;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class ExtendedTypeReference
        : TypeReference
        , IEquatable<ExtendedTypeReference>
    {
        public ExtendedTypeReference(
            IExtendedType type,
            TypeContext context,
            string? scope = null)
            : base(context, scope)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public IExtendedType Type { get; }

        public bool Equals(ExtendedTypeReference? other)
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

            if (other is ExtendedTypeReference c)
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

            if (obj is ExtendedTypeReference c)
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
            return $"{Context}: {Type.ToString()}";
        }

        public ExtendedTypeReference WithType(IExtendedType type)
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

        public ExtendedTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return TypeReference.Create(
                Type,
                context,
                Scope);
        }

        public ExtendedTypeReference WithScope(string? scope = null)
        {
            return TypeReference.Create(
                Type,
                Context,
                scope);
        }

        public ExtendedTypeReference With(
            IExtendedType? type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default)
        {
            return TypeReference.Create(
                type ?? Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope);
        }
    }
}
