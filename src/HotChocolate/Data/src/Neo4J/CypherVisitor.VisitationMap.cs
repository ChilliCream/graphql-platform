namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void Enter(Match match)
        {
            if (match.IsOptional())
            {
                _builder.Write("OPTIONAL ");
            }
            _builder.Write("MATCH ");
        }

        public void Leave(Match match)
        {
            _builder.Write(" ");
        }

        public void Enter(Where where)
        {
            _builder.Write(" WHERE ");
        }

        public void Enter(Create create)
        {
            _builder.Write("CREATE ");
        }

        public void Leave(Create create)
        {
            _builder.Write(" ");
        }
    }
}