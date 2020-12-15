using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void Enter(Visitable visitable)
        {
            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    EnterVisitable((Match)visitable);
                    break;
                case ClauseKind.Where:
                    EnterVisitable((Where)visitable);
                    break;
                case ClauseKind.Create:
                    EnterVisitable((Create)visitable);
                    break;
                case ClauseKind.Node:
                    EnterVisitable((Node)visitable);
                    break;
                case ClauseKind.SymbolicName:
                    EnterVisitable((SymbolicName)visitable);
                    break;
                case ClauseKind.NodeLabel:
                    EnterVisitable((NodeLabel)visitable);
                    break;
                case ClauseKind.Properties:
                    EnterVisitable((Properties)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}
