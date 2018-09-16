using System;

namespace HotChocolate.Resolvers
{
    internal sealed class DirectiveMiddlewareReference
        : IDirectiveMiddleware
        , IEquatable<DirectiveMiddlewareReference>
    {
        public DirectiveMiddlewareReference(
            string directiveName,
            MiddlewareKind kind)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            DirectiveName = directiveName;
            Kind = kind;
        }

        public string DirectiveName { get; }

        public MiddlewareKind Kind { get; }

        public bool Equals(DirectiveMiddlewareReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.DirectiveName
                    .Equals(DirectiveName, StringComparison.Ordinal)
                && other.Kind.Equals(Kind);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as DirectiveMiddlewareReference);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = DirectiveName.GetHashCode() * 397;
                hashCode ^= Kind.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{DirectiveName} {Kind}";
        }
    }
}
