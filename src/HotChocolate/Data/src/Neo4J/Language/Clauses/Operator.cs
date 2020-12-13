namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public class Operator : Visitable
    {
        // Mathematical operators
        public readonly static Operator Addition = new Operator("+");
        public readonly static Operator Subtraction = new Operator("-");
        public readonly static Operator Multiplication = new Operator("*");
        public readonly static Operator Division = new Operator("/");
        public readonly static Operator Modulo = new Operator("%");
        public readonly static Operator Exponent = new Operator("^");

        // Comparison operators
        public readonly static Operator Equality = new Operator("=");
        public readonly static Operator InEquality = new Operator("<>");
        public readonly static Operator LessThan = new Operator("<");
        public readonly static Operator GreaterThan = new Operator(">");
        public readonly static Operator LessThanOrEqualTo = new Operator("<=");
        public readonly static Operator GreaterThanOrEqualTo = new Operator(">=");
        public readonly static Operator IsNull = new Operator("IS NULL", Type.Postfix);
        public readonly static Operator IsNotNull = new Operator("IS NOT NULL", Type.Postfix);

        // String specific comparison operators
        public readonly static Operator StartsWith = new Operator("STARTS WITH");
        public readonly static Operator EndsWith = new Operator("ENDS WITH");
        public readonly static Operator Contains = new Operator("CONTAINS");

        // Boolean operators
        public readonly static Operator And = new Operator("AND");
        public readonly static Operator Or = new Operator("OR");
        public readonly static Operator XOr = new Operator("XOR");
        public readonly static Operator Not = new Operator("NOT", Type.Prefix);

        // String operators
        public readonly static Operator Concat = new Operator("+");
        public readonly static Operator Matches = new Operator("=~");

        // List operators
        public readonly static Operator In = new Operator("IN");

        // Property operators
        public readonly static Operator Set = new Operator("=", Type.Property);
        public readonly static Operator Get = new Operator(".", Type.Property);
        public readonly static Operator Mutate = new Operator("+=", Type.Property);

        // Node operators
        public readonly static Operator SetLabel = new Operator("", Type.Label);
        public readonly static Operator RemoveLabel = new Operator("", Type.Label);

        public override ClauseKind Kind => ClauseKind.Operator;

        private readonly string _representation;
        private readonly Type _type;

        public bool IsUnary() => _type != Type.Binary;

        public new Type GetType() => _type;

        public string GetRepresentation() => _representation;

        public Operator(string rep, Type type)
        {
            _representation = rep;
            _type = type;
        }
        public Operator(string rep) : this(rep, Type.Binary) { }

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
