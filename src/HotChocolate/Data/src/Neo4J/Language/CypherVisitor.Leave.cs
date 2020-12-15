using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void Leave(Visitable visitable)
        {
            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    LeaveVistable((Match)visitable);
                    break;
                case ClauseKind.Create:
                    LeaveVistable((Create)visitable);
                    break;
                case ClauseKind.Node:
                    LeaveVistable((Node)visitable);
                    break;
                case ClauseKind.Properties:
                    LeaveVistable((Properties)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}
