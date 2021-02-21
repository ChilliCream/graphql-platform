using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Leave(IVisitable visitable)
        {
            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    LeaveVisitable((Match)visitable);
                    break;
                case ClauseKind.Create:
                    LeaveVisitable((Create)visitable);
                    break;
                case ClauseKind.Node:
                    LeaveVisitable((Node)visitable);
                    break;
                case ClauseKind.MapExpression:
                    LeaveVisitable((MapExpression)visitable);
                    break;
                case ClauseKind.CompoundCondition:
                    LeaveVisitable((CompoundCondition)visitable);
                    break;
                case ClauseKind.NestedExpression:
                    LeaveVisitable((NestedExpression)visitable);
                    break;
                case ClauseKind.PatternComprehension:
                    LeaveVisitable((PatternComprehension)visitable);
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
                case ClauseKind.Operator:
                    break;
                case ClauseKind.StatementPrefix:
                    break;
                case ClauseKind.Comparison:
                    break;
                case ClauseKind.KeyValueMapEntry:
                    break;
                case ClauseKind.MapProjection:
                    break;
                case ClauseKind.Properties:
                    break;
                case ClauseKind.KeyValueSeparator:
                    break;
                case ClauseKind.Property:
                    break;
                case ClauseKind.PropertyLookup:
                    break;
                case ClauseKind.SortItem:
                    break;
                case ClauseKind.Literal:
                    break;
                case ClauseKind.BooleanLiteral:
                    break;
                case ClauseKind.StringLiteral:
                    break;
                case ClauseKind.ExpressionList:
                    break;
                case ClauseKind.SymbolicName:
                    break;
                case ClauseKind.NodeLabel:
                    break;
                case ClauseKind.NodeLabels:
                    break;
                case ClauseKind.Operation:
                    break;
                case ClauseKind.Relationship:
                    break;
                case ClauseKind.OptionalMatch:
                    break;
                case ClauseKind.Return:
                    break;
                case ClauseKind.With:
                    break;
                case ClauseKind.Unwind:
                    break;
                case ClauseKind.Where:
                    break;
                case ClauseKind.YieldItems:
                    break;
                case ClauseKind.Exists:
                    break;
                case ClauseKind.Distinct:
                    break;
                case ClauseKind.OrderBy:
                    break;
                case ClauseKind.Skip:
                    break;
                case ClauseKind.Limit:
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
