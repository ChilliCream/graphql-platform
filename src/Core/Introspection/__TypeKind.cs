using System;

namespace HotChocolate.Introspection
{
    internal sealed class __TypeKind
        : IEquatable<__TypeKind>
    {
        private readonly string _stringRepresentation;

        private __TypeKind(string stringRepresentation)
        {
            _stringRepresentation = stringRepresentation;
        }

        public bool Equals(__TypeKind other)
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

        public static __TypeKind Scalar { get; } = new __TypeKind("SCALAR");
        public static __TypeKind Object { get; } = new __TypeKind("OBJECT");
        public static __TypeKind Interface { get; } = new __TypeKind("INTERFACE");
        public static __TypeKind Union { get; } = new __TypeKind("UNION");
        public static __TypeKind Enum { get; } = new __TypeKind("ENUM");
        public static __TypeKind InputObject { get; } = new __TypeKind("INPUT_OBJECT");
        public static __TypeKind List { get; } = new __TypeKind("LIST");
        public static __TypeKind NonNull { get; } = new __TypeKind("NON_NULL");
    }
}