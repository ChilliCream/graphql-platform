using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        private void EnterVisitable(Match match)
        {
            if (match.IsOptional())
            {
                _writer.Write("OPTIONAL ");
            }
            _writer.Write("MATCH ");
        }

        private void LeaveVisitable(Match match)
        {
            _writer.Write(" ");
        }

        private void EnterVisitable(Where where)
        {
            _writer.Write(" WHERE ");
        }

        private void EnterVisitable(Exists exists)
        {
            _writer.Write(" EXISTS ");
        }

        private void EnterVisitable(Create create)
        {
            _writer.Write("CREATE ");
        }

        private void LeaveVisitable(Create create)
        {
            _writer.Write(" ");
        }

        private void EnterVisitable(Node node)
        {
            _writer.Write("(");
            _skipNodeContent = _visitedNamed.Contains(node);

            if (!_skipNodeContent) return;
            var symbolicName = node.GetSymbolicName()?.GetValue() ?? node.GetRequiredSymbolicName().GetValue();
            _writer.Write(symbolicName);

        }

        private void LeaveVisitable(Node node)
        {
            _writer.Write(")");

            _skipNodeContent = false;
        }

        private void EnterVisitable(SymbolicName symbolicName)
        {
            _writer.Write(symbolicName.GetValue());
        }

        private void EnterVisitable(NodeLabel nodeLabel)
        {
            _writer.Write(Symbol.Colon);
            _writer.Write(nodeLabel.GetValue());
        }

        private void EnterVisitable(PropertyLookup propertyLookup)
        {
            _writer.Write(propertyLookup.IsDynamicLookup() ? "[" : ".");
        }

        private void LeaveVisitable(PropertyLookup propertyLookup)
        {
            if (propertyLookup.IsDynamicLookup()) {
                _writer.Write("]");
            }
        }
        private void EnterVisitable(Operation operation)
        {

            if (operation.NeedsGrouping()) {
                _writer.Write("(");
            }
        }

        private void EnterVisitable(Operator op)
        {
            Operator.Type type = op.GetType();
            if (type == Operator.Type.Label) {
                return;
            }
            if (type != Operator.Type.Prefix && op != Operator.Exponent) {
                _writer.Write(" ");
            }
            _writer.Write(op.GetRepresentation());
            if (type != Operator.Type.Postfix && op != Operator.Exponent) {
                _writer.Write(" ");
            }
        }

        private void LeaveVisitable(Operation operation)
        {

            if (operation.NeedsGrouping()) {
                _writer.Write(")");
            }
        }

        private void EnterVisitable(Properties properties)
        {
            _writer.Write(" ");
        }

        private void EnterVisitable(MapExpression map)
        {
            _writer.Write(" {");
        }

        private void EnterVisitable(KeyValueMapEntry map)
        {
            _writer.Write(map.GetKey());
            _writer.Write(": ");
        }

        private void LeaveVisitable(MapExpression map)
        {
            _writer.Write("}");
        }

        private void EnterVisitable(PatternComprehension patternComprehension)
        {
            _writer.Write("[");
        }

        private void LeaveVisitable(PatternComprehension patternComprehension)
        {
            _writer.Write("]");
        }


        private void EnterVisitable(ILiteral literal)
        {
            _writer.Write(literal.AsString());
        }

        private void EnterVisitable(CompoundCondition compoundCondition)
        {
            _writer.Write("(");
        }

        private void LeaveVisitable(CompoundCondition compoundCondition)
        {
            _writer.Write(")");
        }

        private void EnterVisitable(NestedExpression nestedExpression)
        {
            _writer.Write("(");
        }

        private void LeaveVisitable(NestedExpression nestedExpression)
        {
            _writer.Write(")");
        }

        private void EnterVisitable(Return @return)
        {
            _writer.Write("RETURN ");
        }

        private void EnterVisitable(Distinct distinct)
        {
            _writer.Write("DISTINCT ");
        }
        private void EnterVisitable(OrderBy orderBy)
        {
            _writer.Write(" ORDER BY ");
        }

        private void EnterVisitable(Skip skip)
        {
            _writer.Write(" SKIP ");
        }

        private void EnterVisitable(Limit limit)
        {
            _writer.Write(" LIMIT ");
        }

        private void EnterVisitable(AliasedExpression aliased)
        {
            _writer.Write(" AS ");
            _writer.Write(aliased.GetAlias());
        }

        private void EnterVisitable(RelationshipDetails details)
        {
            RelationshipDirection direction = details.GetDirection();

            _writer.Write(direction.GetLeftSymbol());
            if (details.HasContent())
            {
                _writer.Write("[");
            }
        }

        private void LeaveVisitable(RelationshipDetails details)
        {
            RelationshipDirection direction = details.GetDirection();

            if (details.HasContent())
            {
                _writer.Write("]");
            }
            _writer.Write(direction.GetRightSymbol());
        }

        private void EnterVisitable(RelationshipTypes types)
        {
            _writer.Write(
                types.GetValues()
                    .Aggregate(string.Empty,
                        (partialPhrase, word) => $"{partialPhrase}{Symbol.Pipe}:{word}")
                    .TrimStart(Symbol.Pipe.ToCharArray()));
        }

        private void EnterVisitable(RelationshipLength length)
        {
            var minimum = length.GetMinimum();
            var maximum = length.GetMaximum();

            if (length.IsUnbounded()) {
                _writer.Write("*");
                return;
            }

            if (minimum == null && maximum == null) {
                return;
            }

            _writer.Write("*");
            if (minimum != null) {
                _writer.Write(minimum.ToString());
            }
            _writer.Write("..");
            if (maximum != null) {
                _writer.Write(maximum.ToString());
            }
        }

        private void EnterVisitable(ListExpression expression)
        {
            _writer.Write("[");
        }

        private void LeaveVisitable(ListExpression expression)
        {
            _writer.Write("[");
        }

        private void EnterVisitable(SortDirection sortDirection)
        {
            _writer.Write(" ");
            _writer.Write(sortDirection.GetSymbol());
        }

        private void EnterVisitable(With with)
        {
            _writer.Write("WITH ");
        }

        private void LeaveVisitable(With with)
        {
            _writer.Write(" ");
        }

        private void EnterVisitable(Delete delete)
        {
            if (delete.IsDetach())
                _writer.Write("DETACH ");

            _writer.Write("DELETE ");
        }

        private void LeaveVisitable(Delete delete)
        {
            _writer.Write(" ");
        }

        void EnterVisitable(FunctionInvocation functionInvocation)
        {
            _writer.Write(functionInvocation.GetFunctionName());
            _writer.Write("(");
        }

        private void LeaveVisitable(FunctionInvocation functionInvocation)
        {
            _writer.Write(")");
        }
    }
}
