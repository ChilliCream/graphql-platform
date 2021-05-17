namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public partial class Operator
    {
        // Aggregation
        public static readonly Operator Distinct = new("DISTINCT");

        // Mathematical operators
        public static readonly Operator Addition = new("+");

        public static readonly Operator Subtraction = new("-");

        public static readonly Operator Multiplication = new("*");

        public static readonly Operator Division = new("/");

        public static readonly Operator Modulo = new("%");

        public static readonly Operator Exponent = new("^");

        // Comparison operators
        public static readonly Operator Equality = new("=");

        public static readonly Operator InEquality = new("<>");

        public static readonly Operator LessThan = new("<");

        public static readonly Operator GreaterThan = new(">");

        public static readonly Operator LessThanOrEqualTo = new("<=");

        public static readonly Operator GreaterThanOrEqualTo = new(">=");

        public static readonly Operator IsNull = new("IS NULL", Type.Postfix);

        public static readonly Operator IsNotNull = new("IS NOT NULL", Type.Postfix);

        // String specific comparison operators
        public static readonly Operator StartsWith = new("STARTS WITH");

        public static readonly Operator EndsWith = new("ENDS WITH");

        public static readonly Operator Contains = new("CONTAINS");

        // Boolean operators
        public static readonly Operator And = new("AND");

        public static readonly Operator Or = new("OR");

        public static readonly Operator XOr = new("XOR");

        public static readonly Operator Not = new("NOT", Type.Prefix);

        // String operators
        public static readonly Operator Concat = new("+");

        public static readonly Operator Matches = new("=~");

        // List operators
        public static readonly Operator In = new("IN");

        public static readonly Operator NotIn = new("NOT IN");

        // Property operators
        public static readonly Operator Set = new("=", Type.Property);

        public static readonly Operator Get = new(".", Type.Property);

        public static readonly Operator Mutate = new("+=", Type.Property);

        // Node operators
        public static readonly Operator SetLabel = new("", Type.Label);

        public static readonly Operator RemoveLabel = new("", Type.Label);

        // Misc
        public static readonly Operator Eq = new("=");

        public static readonly Operator Pipe = new("|");
    }
}
