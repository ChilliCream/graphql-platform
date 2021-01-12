namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public class Operator : Visitable
    {
        // Aggregation
        public static readonly Operator Distinct = new ("DISTINCT");

        // Mathematical operators
        public static readonly Operator Addition = new ("+");
        public static readonly Operator Subtraction = new ("-");
        public static readonly Operator Multiplication = new Operator("*");
        public static readonly Operator Division = new Operator("/");
        public static readonly Operator Modulo = new Operator("%");
        public static readonly Operator Exponent = new Operator("^");

        // Comparison operators
        public static readonly Operator Equality = new Operator("=");
        public static readonly Operator InEquality = new Operator("<>");
        public static readonly Operator LessThan = new Operator("<");
        public static readonly Operator GreaterThan = new Operator(">");
        public static readonly Operator LessThanOrEqualTo = new Operator("<=");
        public static readonly Operator GreaterThanOrEqualTo = new Operator(">=");
        public static readonly Operator IsNull = new Operator("IS NULL", Type.Postfix);
        public static readonly Operator IsNotNull = new Operator("IS NOT NULL", Type.Postfix);

        // String specific comparison operators
        public static readonly Operator StartsWith = new Operator("STARTS WITH");
        public static readonly Operator EndsWith = new Operator("ENDS WITH");
        public static readonly Operator Contains = new Operator("CONTAINS");

        // Boolean operators
        public static readonly Operator And = new Operator("AND");
        public static readonly Operator Or = new Operator("OR");
        public static readonly Operator XOr = new Operator("XOR");
        public static readonly Operator Not = new Operator("NOT", Type.Prefix);

        // String operators
        public static readonly Operator Concat = new Operator("+");
        public static readonly Operator Matches = new Operator("=~");

        // List operators
        public static readonly Operator In = new Operator("IN");

        // Property operators
        public static readonly Operator Set = new Operator("=", Type.Property);
        public static readonly Operator Get = new Operator(".", Type.Property);
        public static readonly Operator Mutate = new Operator("+=", Type.Property);

        // Node operators
        public static readonly Operator SetLabel = new Operator("", Type.Label);
        public static readonly Operator RemoveLabel = new Operator("", Type.Label);

        // Misc
        public static readonly Operator Eq = new Operator("=");
        public static readonly Operator Pipe = new Operator("|");

        public override ClauseKind Kind => ClauseKind.Operator;

        private readonly string _representation;
        private readonly Type _type;

        public bool IsUnary() => _type != Type.Binary;

        public new Type GetType() => _type;

        public string GetRepresentation() => _representation;

        private Operator(string rep, Type type)
        {
            _representation = rep;
            _type = type;
        }

        private Operator(string rep) : this(rep, Type.Binary) { }

        public enum Type
        {
            Binary,
            Prefix,
            Postfix,
            Property,
            Label
        }
    }
}
