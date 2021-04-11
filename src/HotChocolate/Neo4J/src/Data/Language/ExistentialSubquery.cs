namespace HotChocolate.Data.Neo4J.Language
{
    public class ExistentialSubquery : Condition
    {
        public override ClauseKind Kind => ClauseKind.ExistentialSubquery;

        private readonly Match _fragment;

        public ExistentialSubquery(Match fragment)
        {
            _fragment = fragment;
        }

        public static ExistentialSubquery Exists(Match fragment) => new (fragment);
        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _fragment.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
