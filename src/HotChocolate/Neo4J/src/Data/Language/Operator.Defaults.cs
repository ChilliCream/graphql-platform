namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public partial class Operator
    {
        // Aggregation
        public static Operator Distinct { get; } = new("DISTINCT");

        // Mathematical operators
        public static Operator Addition { get; } = new("+");

        public static Operator Subtraction { get; } = new("-");

        public static Operator Multiplication { get; } = new("*");

        public static Operator Division { get; } = new("/");

        public static Operator Modulo { get; } = new("%");

        public static Operator Exponent { get; } = new("^");

        // Comparison operators
        public static Operator Equality { get; } = new("=");

        public static Operator InEquality { get; } = new("<>");

        public static Operator LessThan { get; } = new("<");

        public static Operator GreaterThan { get; } = new(">");

        public static Operator LessThanOrEqualTo { get; } = new("<=");

        public static Operator GreaterThanOrEqualTo { get; } = new(">=");

        public static Operator IsNull { get; } = new("IS NULL", OperatorType.Postfix);

        public static Operator IsNotNull { get; } = new("IS NOT NULL", OperatorType.Postfix);

        // String specific comparison operators
        public static Operator StartsWith { get; } = new("STARTS WITH");

        public static Operator EndsWith { get; } = new("ENDS WITH");

        public static Operator Contains { get; } = new("CONTAINS");

        // Boolean operators
        public static Operator And { get; } = new("AND");

        public static Operator Or { get; } = new("OR");

        public static Operator XOr { get; } = new("XOR");

        public static Operator Not { get; } = new("NOT", OperatorType.Prefix);

        // String operators
        public static Operator Concat { get; } = new("+");

        public static Operator Matches { get; } = new("=~");

        // List operators
        public static Operator In { get; } = new("IN");

        public static Operator NotIn { get; } = new("NOT IN");

        // Property operators
        public static Operator Set { get; } = new("=", OperatorType.Property);

        public static Operator Get { get; } = new(".", OperatorType.Property);

        public static Operator Mutate { get; } = new("+=", OperatorType.Property);

        // Node operators
        public static Operator SetLabel { get; } = new("", OperatorType.Label);

        public static Operator RemoveLabel { get; } = new("", OperatorType.Label);

        // Misc
        public static Operator Eq { get; } = new("=");

        public static Operator Pipe { get; } = new("|");
    }
}
