namespace Zeus.Introspection
{
    internal sealed class __DirectiveLocation
    {
        private readonly string _stringRepresentation;

        private __DirectiveLocation(string stringRepresentation)
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

        public static __DirectiveLocation Query { get; } = new __DirectiveLocation("QUERY");
        public static __DirectiveLocation Mutation { get; } = new __DirectiveLocation("MUTATION");
        public static __DirectiveLocation Field { get; } = new __DirectiveLocation("FIELD");
        public static __DirectiveLocation FragmentDefinition { get; } = new __DirectiveLocation("FRAGMENT_DEFINITION");
        public static __DirectiveLocation FragmentSpread { get; } = new __DirectiveLocation("FRAGMENT_SPRED");
        public static __DirectiveLocation InlineFragment { get; } = new __DirectiveLocation("INLINE_FRAGMENT");
    }
}