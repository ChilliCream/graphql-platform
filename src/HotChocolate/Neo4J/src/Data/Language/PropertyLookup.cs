namespace HotChocolate.Data.Neo4J.Language
{
    public class PropertyLookup : Expression
    {
        private static readonly PropertyLookup _wildcard = new(Asterisk.Instance, false);

        public PropertyLookup(Expression propertyKeyName, bool isDynamicLookup)
        {
            PropertyKeyName = propertyKeyName;
            IsDynamicLookup = isDynamicLookup;
        }

        public override ClauseKind Kind => ClauseKind.PropertyLookup;

        public Expression PropertyKeyName { get; }

        public bool IsDynamicLookup { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            PropertyKeyName.Visit(cypherVisitor);
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
