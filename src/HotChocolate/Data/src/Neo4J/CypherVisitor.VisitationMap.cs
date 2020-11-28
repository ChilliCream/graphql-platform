using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void EnterVisitable(Match match)
        {
            if (match.IsOptional())
            {
                _builder.Write("OPTIONAL ");
            }
            _builder.Write("MATCH ");
        }

        public void LeaveVistable(Match match)
        {
            _builder.Write(" ");
        }

        public void EnterVisitable(Where where)
        {
            _builder.Write(" WHERE ");
        }

        public void EnterVisitable(Create create)
        {
            _builder.Write("CREATE ");
        }

        public void LeaveVistable(Create create)
        {
            _builder.Write(" ");
        }
    }
}
