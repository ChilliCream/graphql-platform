using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Leave(IVisitable visitable)
        {
            if (Equals(_currentVisitedElements.First?.Value, visitable))
            {
                PostLeave(visitable ?? throw new ArgumentNullException(nameof(visitable)));
                _currentVisitedElements.RemoveFirst();
            }

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
                case ClauseKind.RelationshipDetails:
                    LeaveVisitable((RelationshipDetails)visitable);
                    break;
                case ClauseKind.PropertyLookup:
                    LeaveVisitable((PropertyLookup)visitable);
                    break;
                case ClauseKind.ListExpression:
                    LeaveVisitable((ListExpression)visitable);
                    break;
                case ClauseKind.With:
                    LeaveVisitable((With)visitable);
                    break;
                case ClauseKind.Delete:
                    LeaveVisitable((Delete)visitable);
                    break;
                case ClauseKind.FunctionInvocation:
                    LeaveVisitable((FunctionInvocation)visitable);
                    break;
                case ClauseKind.Operation:
                    LeaveVisitable((Operation)visitable);
                    break;
                case ClauseKind.Remove:
                    LeaveVisitable((Remove)visitable);
                    break;
                case ClauseKind.ListComprehension:
                    LeaveVisitable((ListComprehension)visitable);
                    break;
                case ClauseKind.AliasedExpression:
                case ClauseKind.SortDirection:
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
                case ClauseKind.Relationship:
                case ClauseKind.OptionalMatch:
                case ClauseKind.Return:
                case ClauseKind.Unwind:
                case ClauseKind.YieldItems:
                case ClauseKind.Exists:
                case ClauseKind.Distinct:
                case ClauseKind.OrderBy:
                case ClauseKind.Skip:
                case ClauseKind.Limit:
                case ClauseKind.Set:
                case ClauseKind.Foreach:
                case ClauseKind.Merge:
                case ClauseKind.Union:
                case ClauseKind.Use:
                case ClauseKind.LoadCsv:
                case ClauseKind.Condition:
                case ClauseKind.Default:
                case ClauseKind.DistinctExpression:
                case ClauseKind.RelationshipChain:
                case ClauseKind.RelationshipPatternCondition:
                case ClauseKind.Statement:
                case ClauseKind.RelationshipLength:
                case ClauseKind.RelationshipTypes:
                case ClauseKind.ExpressionCondition:
                case ClauseKind.HasLabelCondition:
                case ClauseKind.Where:
                case ClauseKind.BooleanFunctionCondition:
                case ClauseKind.NotCondition:
                case ClauseKind.ProjectionBody:
                case ClauseKind.ListPredicate:
                case ClauseKind.UnionPart:
                case ClauseKind.UnionQuery:
                    break;
            }
        }
    }
}
