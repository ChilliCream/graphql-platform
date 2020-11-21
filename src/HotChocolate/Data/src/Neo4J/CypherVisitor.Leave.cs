namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void Leave(Visitable visitable)
        {
            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    Leave((Match)visitable);
                    break;
                case ClauseKind.Create:
                    Leave((Create)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}