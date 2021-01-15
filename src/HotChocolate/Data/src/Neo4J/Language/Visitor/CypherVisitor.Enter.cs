using System;

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
                case ClauseKind.Return:
                    EnterVisitable((Return)visitable);
                    break;
                case ClauseKind.Distinct:
                    EnterVisitable((Distinct)visitable);
                    break;
                case ClauseKind.OrderBy:
                    EnterVisitable((OrderBy)visitable);
                    break;
                case ClauseKind.Skip:
                    EnterVisitable((Skip)visitable);
                    break;
                case ClauseKind.Limit:
                    EnterVisitable((Limit)visitable);
                    break;
                case ClauseKind.Exists:
                    EnterVisitable((Exists)visitable);
                    break;
                case ClauseKind.Expression:
                    break;
                case ClauseKind.AliasedExpression:
                    break;
                case ClauseKind.Visitable:
                    break;
                case ClauseKind.TypedSubtree:
                    break;
                case ClauseKind.Pattern:
                    break;
                case ClauseKind.ExcludePattern:
                    break;
                case ClauseKind.StatementPrefix:
                    break;
                case ClauseKind.Comparison:
                    break;
                case ClauseKind.MapProjection:
                    break;
                case ClauseKind.Property:
                    break;
                case ClauseKind.SortItem:
                    break;
                case ClauseKind.BooleanLiteral:
                    break;
                case ClauseKind.StringLiteral:
                    break;
                case ClauseKind.ExpressionList:
                    break;
                case ClauseKind.NodeLabels:
                    break;
                case ClauseKind.Operation:
                    break;
                case ClauseKind.Relationship:
                    break;
                case ClauseKind.OptionalMatch:
                    break;
                case ClauseKind.With:
                    break;
                case ClauseKind.Unwind:
                    break;
                case ClauseKind.YieldItems:
                    break;
                case ClauseKind.Delete:
                    break;
                case ClauseKind.Set:
                    break;
                case ClauseKind.Remove:
                    break;
                case ClauseKind.Foreach:
                    break;
                case ClauseKind.Merge:
                    break;
                case ClauseKind.Union:
                    break;
                case ClauseKind.Use:
                    break;
                case ClauseKind.LoadCsv:
                    break;
                case ClauseKind.Condition:
                    break;
                case ClauseKind.Default:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
