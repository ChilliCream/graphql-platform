namespace HotChocolate.Data.Neo4J.Language
{
    public class PropertyLookup : Expression
    {
        private readonly Expression _propertyKeyName;
        private readonly bool _dynamicLookup;
        private static readonly PropertyLookup _wildcard = new(Asterisk.Instance, false);

        public PropertyLookup(Expression propertyKeyName, bool dynamicLookup)
        {
            _propertyKeyName = propertyKeyName;
            _dynamicLookup = dynamicLookup;
        }

        public override ClauseKind Kind => ClauseKind.PropertyLookup;

        public bool IsDynamicLookup() => _dynamicLookup;

        public SymbolicName GetPropertyKeyName()
        {
            Ensure.IsTrue(
                this != _wildcard,
                "The wildcard property lookup does not reference a specific property!");

            return (SymbolicName)_propertyKeyName;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _propertyKeyName.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public static PropertyLookup Wildcard() => _wildcard;

        public static PropertyLookup ForName(string name)
        {
            return new(SymbolicName.Unsafe(name), false);
        }

        public static PropertyLookup ForExpression(Expression expression)
        {
            Ensure.IsNotNull(expression, "The expression is required");
            return new PropertyLookup(expression, true);
        }
    }
}
