namespace HotChocolate.Data.Neo4J.Language
{
    public class StatementPrefix : Visitable
    {
        public readonly static StatementPrefix Create = new StatementPrefix("CREATE");
        public readonly static StatementPrefix Match = new StatementPrefix("MATCH");
        public readonly static StatementPrefix OptionalMatch = new StatementPrefix("OPTIONAL MATCH");
        public readonly static StatementPrefix With = new StatementPrefix("WITH");
        public readonly static StatementPrefix Call = new StatementPrefix("CALL");
        public readonly static StatementPrefix Merge = new StatementPrefix("MERGE");
        public readonly static StatementPrefix DetachDelete = new StatementPrefix("DETACH DELETE");
        public override ClauseKind Kind => ClauseKind.StatementPrefix;
        private readonly string _representation;
        public string GetRepresentation() => _representation;

        public StatementPrefix(string rep)
        {
            _representation = rep;
        }
    }
}
