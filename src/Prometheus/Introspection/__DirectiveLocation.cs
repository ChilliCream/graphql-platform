using System;

namespace Prometheus.Introspection
{
    internal sealed class __DirectiveLocation
        : IEquatable<__DirectiveLocation>
    {
        private readonly string _stringRepresentation;

        private __DirectiveLocation(string stringRepresentation)
        {
            _stringRepresentation = stringRepresentation;
        }

        public bool Equals(__DirectiveLocation other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return _stringRepresentation.Equals(
                other._stringRepresentation,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            return Equals(obj as __DirectiveLocation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return _stringRepresentation.GetHashCode() * 7;
            }
        }

        public override string ToString()
        {
            return _stringRepresentation;
        }

        public static __DirectiveLocation Query { get; } = new __DirectiveLocation("QUERY");
        public static __DirectiveLocation Mutation { get; } = new __DirectiveLocation("MUTATION");
        public static __DirectiveLocation Field { get; } = new __DirectiveLocation("FIELD");
        public static __DirectiveLocation FragmentDefinition { get; } = new __DirectiveLocation("FRAGMENT_DEFINITION");
        public static __DirectiveLocation FragmentSpread { get; } = new __DirectiveLocation("FRAGMENT_SPRED");
        public static __DirectiveLocation InlineFragment { get; } = new __DirectiveLocation("INLINE_FRAGMENT");
    }
}