namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Leave(IVisitable visitable)
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
                case ClauseKind.MapExpression:
                    LeaveVistable((MapExpression)visitable);
                    break;
                case ClauseKind.CompoundCondition:
                    LeaveVisitable((CompoundCondition)visitable);
                    break;
                case ClauseKind.NestedExpression:
                    LeaveVisitable((NestedExpression)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}
