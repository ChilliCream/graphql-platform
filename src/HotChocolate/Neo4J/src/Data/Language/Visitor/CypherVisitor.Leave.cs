using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Leave(IVisitable visitable)
        {
            //if (!Equals(_currentVisitedElements.Peek(), visitable)) return;
            //PostLeave(visitable);
            //_currentVisitedElements.Dequeue();

            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    LeaveVisitable((Match)visitable);
                    break;
                case ClauseKind.Where:
                    LeaveVisitable((Where)visitable);
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
                case ClauseKind.RelationshipDetails:
                    LeaveVisitable((RelationshipDetails)visitable);
                    break;
                case ClauseKind.PropertyLookup:
                    LeaveVisitable((PropertyLookup)visitable);
                    break;
                case ClauseKind.ListExpression:
                    LeaveVisitable((ListExpression)visitable);
                    break;
                case ClauseKind.AliasedExpression:
                case ClauseKind.Expression:
                case ClauseKind.Visitable:
                case ClauseKind.TypedSubtree:
                case ClauseKind.Pattern:
                case ClauseKind.ExcludePattern:
                case ClauseKind.Operator:
                case ClauseKind.StatementPrefix:
                case ClauseKind.Comparison:
                case ClauseKind.KeyValueMapEntry:
                case ClauseKind.MapProjection:
                case ClauseKind.Properties:
                case ClauseKind.Property:
                case ClauseKind.SortItem:
                case ClauseKind.Literal:
                case ClauseKind.BooleanLiteral:
                case ClauseKind.StringLiteral:
                case ClauseKind.ExpressionList:
                case ClauseKind.SymbolicName:
                case ClauseKind.NodeLabel:
                case ClauseKind.NodeLabels:
                case ClauseKind.Operation:
                case ClauseKind.Relationship:
                case ClauseKind.OptionalMatch:
                case ClauseKind.Return:
                case ClauseKind.With:
                case ClauseKind.Unwind:
                case ClauseKind.YieldItems:
                case ClauseKind.Exists:
                case ClauseKind.Distinct:
                case ClauseKind.OrderBy:
                case ClauseKind.Skip:
                case ClauseKind.Limit:
                case ClauseKind.Delete:
                case ClauseKind.Set:
                case ClauseKind.Remove:
                case ClauseKind.Foreach:
                case ClauseKind.Merge:
                case ClauseKind.Union:
                case ClauseKind.Use:
                case ClauseKind.LoadCsv:
                case ClauseKind.Condition:
                case ClauseKind.Default:
                case ClauseKind.Arguments:
                case ClauseKind.DistinctExpression:
                case ClauseKind.RelationshipChain:
                case ClauseKind.RelationshipPatternCondition:
                case ClauseKind.Statement:
                case ClauseKind.RelationshipLength:
                case ClauseKind.RelationshipTypes:
                case ClauseKind.ExpressionCondition:
                case ClauseKind.HasLabelCondition:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
