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
                case ClauseKind.AliasedExpression:
                case ClauseKind.Expression:
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
                    break;
                case ClauseKind.RelationshipTypes:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
