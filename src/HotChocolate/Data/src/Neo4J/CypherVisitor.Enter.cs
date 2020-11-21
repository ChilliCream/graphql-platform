namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void Enter(Visitable visitable)
        {
            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    Enter((Match)visitable);
                    break;
                case ClauseKind.Where:
                    Enter((Where)visitable);
                    break;
                case ClauseKind.Create:
                    Enter((Create)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}