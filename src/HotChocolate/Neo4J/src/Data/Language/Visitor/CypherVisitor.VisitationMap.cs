using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        private void EnterVisitable(Match match)
        {
            if (match.IsOptional)
            {
                _writer.Write("OPTIONAL ");
            }

            _writer.Write("MATCH ");
        }

        private void LeaveVisitable(Match _) => _writer.Write(" ");

        private void EnterVisitable(Where _) => _writer.Write(" WHERE ");

        private void EnterVisitable(Exists _) => _writer.Write(" EXISTS ");

        private void EnterVisitable(Node node)
        {
            _writer.Write("(");
            _skipNodeContent = _visitedNamed.Contains(node);

            if (!_skipNodeContent)
            {
                return;
            }

            string symbolicName =
                node.SymbolicName?.Value ??
                node.RequiredSymbolicName.Value;

            _writer.Write(symbolicName);
        }

        private void LeaveVisitable(Node _)
        {
            _writer.Write(")");
            _skipNodeContent = false;
        }

        private void EnterVisitable(SymbolicName symbolicName) =>
            _writer.Write(symbolicName.Value);

        private void EnterVisitable(NodeLabel nodeLabel)
        {
            _writer.Write(Symbol.Colon);
            _writer.Write(nodeLabel.Value);
        }

        private void EnterVisitable(PropertyLookup propertyLookup) =>
            _writer.Write(propertyLookup.IsDynamicLookup ? "[" : ".");

        private void LeaveVisitable(PropertyLookup propertyLookup)
        {
            if (propertyLookup.IsDynamicLookup)
            {
                _writer.Write("]");
            }
        }

        private void EnterVisitable(Operation operation)
        {
            if (operation.NeedsGrouping())
            {
                _writer.Write("(");
            }
        }

        private void EnterVisitable(Operator op)
        {
            OperatorType type = op.Type;
            if (type == OperatorType.Label)
            {
                return;
            }

            if (type != OperatorType.Prefix && op != Operator.Exponent)
            {
                _writer.Write(" ");
            }

            _writer.Write(op.Representation);
            if (type != OperatorType.Postfix && op != Operator.Exponent)
            {
                _writer.Write(" ");
            }
        }

        private void LeaveVisitable(Operation operation)
        {
            if (operation.NeedsGrouping())
            {
                _writer.Write(")");
            }
        }

        private void EnterVisitable(Properties _) => _writer.Write(" ");

        private void EnterVisitable(MapExpression _) => _writer.Write(" {");

        private void EnterVisitable(KeyValueMapEntry map)
        {
            _writer.Write(map.Key);
            _writer.Write(": ");
        }

        private void LeaveVisitable(MapExpression _) => _writer.Write("}");

        private void EnterVisitable(PatternComprehension _) => _writer.Write("[");

        private void LeaveVisitable(PatternComprehension _) => _writer.Write("]");

        private void EnterVisitable(ILiteral literal) => _writer.Write(literal.Print());

        private void EnterVisitable(CompoundCondition _) => _writer.Write("(");

        private void LeaveVisitable(CompoundCondition _) => _writer.Write(")");

        private void EnterVisitable(NestedExpression _) => _writer.Write("(");

        private void LeaveVisitable(NestedExpression _) => _writer.Write(")");

        private void EnterVisitable(Return _) => _writer.Write("RETURN ");

        private void EnterVisitable(OrderBy _) => _writer.Write(" ORDER BY ");

        private void EnterVisitable(Skip _) => _writer.Write(" SKIP ");

        private void EnterVisitable(Limit _) => _writer.Write(" LIMIT ");

        private void EnterVisitable(AliasedExpression aliased)
        {
            _writer.Write(" AS ");
            _writer.Write(aliased.Alias);
        }

        private void EnterVisitable(RelationshipDetails details)
        {
            RelationshipDirection direction = details.Direction;

            _writer.Write(direction.LeftSymbol);
            if (details.HasContent())
            {
                _writer.Write("[");
            }
        }

        private void LeaveVisitable(RelationshipDetails details)
        {
            RelationshipDirection direction = details.Direction;

            if (details.HasContent())
            {
                _writer.Write("]");
            }

            _writer.Write(direction.RightSymbol);
        }

        private void EnterVisitable(RelationshipTypes types)
        {
            _writer.Write(
                types
                    .Values
                    .Aggregate(
                        string.Empty,
                        (partialPhrase, word) => $"{partialPhrase}{Symbol.Pipe}:{word}")
                    .TrimStart(Symbol.Pipe.ToCharArray()));
        }

        private void EnterVisitable(RelationshipLength length)
        {
            var minimum = length.Minimum;
            var maximum = length.Maximum;

            if (length.IsUnbounded)
            {
                _writer.Write("*");
                return;
            }

            if (minimum is null && maximum is null)
            {
                return;
            }

            _writer.Write("*");
            if (minimum is not null)
            {
                _writer.Write(minimum.Value.ToString());
            }

            _writer.Write("..");
            if (maximum is not null)
            {
                _writer.Write(maximum.Value.ToString());
            }
        }

        private void EnterVisitable(ListExpression _) => _writer.Write("[");

        private void LeaveVisitable(ListExpression _) => _writer.Write("[");

        private void EnterVisitable(SortDirection sortDirection)
        {
            _writer.Write(" ");
            _writer.Write(sortDirection.Symbol);
        }

        private void EnterVisitable(With _) => _writer.Write("WITH ");

        private void LeaveVisitable(With _) => _writer.Write(" ");

        void EnterVisitable(FunctionInvocation functionInvocation)
        {
            _writer.Write(functionInvocation.FunctionName);
            _writer.Write("(");
        }

        private void LeaveVisitable(FunctionInvocation _) => _writer.Write(")");

        private void EnterVisitable(ListComprehension _) => _writer.Write("[");

        private void LeaveVisitable(ListComprehension _) => _writer.Write("]");
    }
}
