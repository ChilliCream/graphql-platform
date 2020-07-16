using System;

namespace HotChocolate.Resolvers
{
    public sealed class SubscribeResolver
        : FieldReferenceBase
        , IEquatable<SubscribeResolver>
    {
        private FieldReference _fieldReference;

        public SubscribeResolver(
            NameString typeName,
            NameString fieldName,
            SubscribeResolverDelegate resolver)
            : base(typeName, fieldName)
        {
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public SubscribeResolver(
            FieldReference fieldReference,
            SubscribeResolverDelegate resolver)
            : base(fieldReference)
        {
            _fieldReference = fieldReference;
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public SubscribeResolverDelegate Resolver { get; }

        public SubscribeResolver WithTypeName(NameString typeName)
        {
            if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
            {
                return this;
            }

            return new SubscribeResolver(typeName, FieldName, Resolver);
        }

        public SubscribeResolver WithFieldName(NameString fieldName)
        {
            if (string.Equals(FieldName, fieldName, StringComparison.Ordinal))
            {
                return this;
            }

            return new SubscribeResolver(TypeName, fieldName, Resolver);
        }

        public SubscribeResolver WithResolver(SubscribeResolverDelegate resolver)
        {
            if (Equals(Resolver, resolver))
            {
                return this;
            }

            return new SubscribeResolver(TypeName, FieldName, resolver);
        }

        public bool Equals(SubscribeResolver other)
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
