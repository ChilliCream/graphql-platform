namespace HotChocolate.Data.Neo4J.Language
{
    public class MergeAction : Visitable
    {
        public override ClauseKind Kind => ClauseKind.MergeAction;
        private readonly Type _type;
        private readonly Set _set;

        public MergeAction(Type type, Set set)
        {
            _type = type;
            _set = set;
        }

        public Type GetType() => _type;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _set.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public enum Type
        {
            OnCreate,
            OnMatch
        }


    }
}
