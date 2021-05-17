namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public partial class Operator : Visitable
    {
        private Operator(string rep, OperatorType type = OperatorType.Binary)
        {
            Representation = rep;
            Type = type;
        }

        public override ClauseKind Kind => ClauseKind.Operator;

        public bool IsUnary() => Type != OperatorType.Binary;

        public OperatorType Type { get; }

        public string Representation { get; }
    }
}
