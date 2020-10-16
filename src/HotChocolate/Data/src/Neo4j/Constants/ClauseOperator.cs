namespace HotChocolate.Data.Neo4j
{
    internal static class ClauseOperator
    {
        // Mathematical operators
        public const string Addition = "+";
        public const string Subtraction = "-";
        public const string Multiplication = "*";
        public const string Division = "/";
        public const string Modulo = "%";
        public const string Exponent = "^";

        // Comparison operators
        public const string Equality = "=";
        public const string InEquality = "<>";
        public const string LessThan = "<";
        public const string GreaterThan = ">";
        public const string LessThanOrEqualTo = "<=";
        public const string GreaterThanOrEqualTo = ">=";
        public const string IsNull = "IS NULL";
        public const string IsNotNull = "IS NOT NULL";

        // String specific comparison operators
        public const string StartsWith = "STARTS WITH";
        public const string EndsWith = "ENDS WITH";
        public const string Contains = "CONTAINS";


        // Boolean operators
        public const string And = "AND";
        public const string Or = "OR";
        public const string XOr = "XOR";
        public const string Not = "NOT";

        // Set Operators
        public const string Equal = "=";
        public const string AppendEqual = "+=";
    }
}
