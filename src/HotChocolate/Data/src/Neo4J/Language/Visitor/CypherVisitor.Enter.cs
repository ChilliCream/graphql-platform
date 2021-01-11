namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Enter(IVisitable visitable)
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
                case ClauseKind.MapExpression:
                    EnterVisitable((MapExpression)visitable);
                    break;
                case ClauseKind.PropertyLookup:
                    EnterVisitable((PropertyLookup)visitable);
                    break;
                case ClauseKind.Operator:
                    EnterVisitable((Operator)visitable);
                    break;
                case ClauseKind.KeyValueMapEntry:
                    EnterVisitable((KeyValueMapEntry)visitable);
                    break;
                case ClauseKind.Literal:
                    EnterVisitable((ILiteral)visitable);
                    break;
                case ClauseKind.CompoundCondition:
                    EnterVisitable((CompoundCondition)visitable);
                    break;
                case ClauseKind.NestedExpression:
                    EnterVisitable((NestedExpression)visitable);
                    break;
                case ClauseKind.KeyValueSeparator:
                    EnterVisitable((KeyValueSeparator)visitable);
                    break;
                case 0:
                    break;
            }
        }
    }
}
