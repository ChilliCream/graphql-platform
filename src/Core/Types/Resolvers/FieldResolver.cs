using System;

namespace HotChocolate.Resolvers
{
    public sealed class FieldResolver
        : FieldReferenceBase
        , IEquatable<FieldResolver>
    {
        private FieldReference _fieldReference;

        public FieldResolver(
            NameString typeName,
            NameString fieldName,
            FieldResolverDelegate resolver)
            : base(typeName, fieldName)
        {
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public FieldResolver(
            FieldReference fieldReference,
            FieldResolverDelegate resolver)
            : base(fieldReference)
        {
            _fieldReference = fieldReference;
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public FieldResolverDelegate Resolver { get; }

        public FieldResolver WithTypeName(NameString typeName)
        {
            if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldResolver(typeName, FieldName, Resolver);
        }

        public FieldResolver WithFieldName(NameString fieldName)
        {
            if (string.Equals(FieldName, fieldName, StringComparison.Ordinal))
            {
                return this;
            }

            return new FieldResolver(TypeName, fieldName, Resolver);
        }

        public FieldResolver WithResolver(FieldResolverDelegate resolver)
        {
            if (Equals(Resolver, resolver))
            {
                return this;
            }

            return new FieldResolver(TypeName, FieldName, resolver);
        }

        public bool Equals(FieldResolver other)
        {
            return IsEqualTo(other);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return IsReferenceEqualTo(obj)
                || IsEqualTo(obj as FieldResolver);
        }

        private bool IsEqualTo(FieldResolver other)
        {
            return base.IsEqualTo(other)
                && other.Resolver.Equals(Resolver);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397)
                    ^ (Resolver.GetHashCode() * 17);
            }
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }

        public FieldReference ToFieldReference()
        {
            if (_fieldReference == null)
            {
                _fieldReference = new FieldReference(TypeName, FieldName);
            }

            return _fieldReference;
        }
    }
}
