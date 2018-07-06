using System;

namespace HotChocolate.Resolvers
{
    // TODO : Rename this class or Execution.FieldResolver
    public sealed class FieldResolver
        : FieldReference
        , IEquatable<FieldResolver>
    {
        public FieldResolver(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
            : base(typeName, fieldName)
        {
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public FieldResolverDelegate Resolver { get; }

        public bool Equals(FieldResolver other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.TypeName.Equals(TypeName)
                && other.FieldName.Equals(FieldName)
                && other.Resolver.Equals(Resolver);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as FieldResolver);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeName.GetHashCode() * 397)
                    ^ (FieldName.GetHashCode() * 17)
                    ^ (Resolver.GetHashCode() * 3);
            }
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }
    }
}
