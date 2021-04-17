using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void Enter(IVisitable visitable)
        {
            if (PreEnter(visitable))
            {
                _currentVisitedElements.AddFirst(visitable);
            }

            switch (visitable.Kind)
            {
                case ClauseKind.Match:
                    EnterVisitable((Match)visitable);
                    break;
                case ClauseKind.Where:
                    EnterVisitable((Where)visitable);
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
                case ClauseKind.PatternComprehension:
                    EnterVisitable((PatternComprehension)visitable);
                    break;
                case ClauseKind.AliasedExpression:
                    EnterVisitable((AliasedExpression)visitable);
                    break;
                case ClauseKind.RelationshipDetails:
                    EnterVisitable((RelationshipDetails)visitable);
                    break;
                case ClauseKind.RelationshipTypes:
                    EnterVisitable((RelationshipTypes)visitable);
                    break;
                case ClauseKind.RelationshipLength:
                    EnterVisitable((RelationshipLength)visitable);
                    break;
                case ClauseKind.ListExpression:
                    EnterVisitable((ListExpression)visitable);
                    break;
                case ClauseKind.SortDirection:
                    EnterVisitable((SortDirection)visitable);
                    break;
                case ClauseKind.With:
                    EnterVisitable((With)visitable);
                    break;
                case ClauseKind.FunctionInvocation:
                    EnterVisitable((FunctionInvocation)visitable);
                    break;
                case ClauseKind.Operation:
                    EnterVisitable((Operation)visitable);
                    break;
                case ClauseKind.ListComprehension:
                    EnterVisitable((ListComprehension)visitable);
                    break;
                case ClauseKind.MapProjection:
                case ClauseKind.Expression:
                case ClauseKind.Visitable:
                case ClauseKind.TypedSubtree:
                case ClauseKind.Pattern:
                case ClauseKind.ExcludePattern:
                case ClauseKind.StatementPrefix:
                case ClauseKind.Comparison:
                case ClauseKind.Property:
                case ClauseKind.SortItem:
                case ClauseKind.BooleanLiteral:
                case ClauseKind.StringLiteral:
                case ClauseKind.ExpressionList:
                case ClauseKind.NodeLabels:
                case ClauseKind.Relationship:
                case ClauseKind.OptionalMatch:
                case ClauseKind.Unwind:
                case ClauseKind.YieldItems:
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
                case ClauseKind.ExpressionCondition:
                case ClauseKind.HasLabelCondition:
                case ClauseKind.BooleanFunctionCondition:
                case ClauseKind.NotCondition:
                case ClauseKind.ProjectionBody:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
