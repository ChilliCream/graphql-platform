using System;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeReference
        : TypeReference
        , IClrTypeReference
        , IEquatable<ClrTypeReference>
        , IEquatable<IClrTypeReference>
    {
        public ClrTypeReference(
            Type type,
            TypeContext context,
            string? scope = null,
            bool[]? nullable = null)
            : base(context, scope, nullable)
        {
            Type = type;
        }

        public Type Type { get; }

        public IClrTypeReference Compile()
        {
            if (IsTypeNullable.HasValue || IsElementTypeNullable.HasValue)
            {
                Type rewritten = DotNetTypeInfoFactory.Rewrite(
                    Type,
                    !(IsTypeNullable ?? false),
                    !(IsElementTypeNullable ?? false));
                return new ClrTypeReference(rewritten, Context);
            }
            return this;
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

            if (!IsEqual(other))
            {
                return false;
            }

            return Type.Equals(other.Type);
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

            if (obj is IClrTypeReference ic)
            {
                return Equals(ic);
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
