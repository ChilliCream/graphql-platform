namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Operators that can be used in Cypher.
    /// https://neo4j.com/docs/cypher-manual/current/syntax/operators/#query-operators-summary
    /// </summary>
    public partial class Operator : Visitable
    {
        private readonly string _representation;
        private readonly Type _type;

        private Operator(string rep, Type type = Type.Binary)
        {
            _representation = rep;
            _type = type;
        }

        public override ClauseKind Kind => ClauseKind.Operator;

        public bool IsUnary() => _type != Type.Binary;

        public new Type GetType() => _type;

        public string GetRepresentation() => _representation;

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
