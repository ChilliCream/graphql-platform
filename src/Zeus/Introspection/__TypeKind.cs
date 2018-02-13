namespace Zeus.Introspection
{
    internal sealed class __TypeKind
    {
        private readonly string _stringRepresentation;

        private __TypeKind(string stringRepresentation)
        {
            _stringRepresentation = stringRepresentation;
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